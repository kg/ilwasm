using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using JSIL;
using JSIL.Ast;
using JSIL.Compiler.Extensibility;
using JSIL.Internal;
using JSIL.Translator;
using ICSharpCode.Decompiler;
using JSIL.Transforms;

namespace WasmSExprEmitter {
    class WasmSExprAssemblyEmitter : IAssemblyEmitter {
        private struct StringTableEntry {
            public byte[] Bytes;
            public int    Offset;
            public int    SizeBytes;

            public StringTableEntry (int offset, string text) {
                Offset = offset;
                Bytes = Encoding.ASCII.GetBytes(text);
                SizeBytes = Bytes.Length + 8;
            }
        }

        private enum PrecedingType {
            None,
            Function,
            Export,
            Global,
            Memory,
            TopLevel
        }

        private IAstEmitter EntryPointAstEmitter;

        public readonly AssemblyTranslator Translator;
        public readonly AssemblyDefinition Assembly;
        public readonly JavascriptFormatter Formatter;

        private readonly Dictionary<string, StringTableEntry> StringTable = new Dictionary<string, StringTableEntry>();
        private int      StringTableSize = 0;

        private PrecedingType Preceding;

        public WasmSExprAssemblyEmitter (AssemblyTranslator translator, AssemblyDefinition assembly, JavascriptFormatter formatter) {
            Translator = translator;
            Assembly = assembly;
            Formatter = formatter;
        }

        public int GetStringOffset (string str) {
            // HACK
            if (str == null)
                str = "";

            StringTableEntry result;
            if (!StringTable.TryGetValue(str, out result)) {
                result = new StringTableEntry(StringTableSize, str);
                StringTableSize += result.SizeBytes;
                StringTable.Add(str, result);
            }

            return result.Offset;
        }

        private void Switch (PrecedingType newType, bool neighborSpacing = false) {
            Formatter.ConditionalNewLine();

            var prior = Preceding;
            Preceding = newType;

            if (prior == PrecedingType.None)
                return;
            else if ((prior == newType) && !neighborSpacing)
                return;

            Formatter.NewLine();
        }

        public void BeginEmitTypeDeclaration (TypeDefinition typedef) {
        }

        public void BeginEmitTypeDefinition (IAstEmitter astEmitter, TypeDefinition typedef, TypeInfo typeInfo, TypeReference baseClass) {
        }

        public void EmitAssemblyEntryPoint (AssemblyDefinition assembly, MethodDefinition entryMethod, MethodSignature signature) {
        }

        public void EmitCustomAttributes (DecompilerContext context, TypeReference declaringType, ICustomAttributeProvider member, IAstEmitter astEmitter, bool standalone = true) {
        }

        public void EmitEvent (DecompilerContext context, IAstEmitter astEmitter, EventDefinition @event, JSRawOutputIdentifier dollar) {
        }

        public void EmitHeader (bool stubbed) {
            Formatter.WriteRaw(";; {0}\n\n", Formatter.Assembly.Name);

            Formatter.WriteRaw("(module \n");
            Formatter.Indent();
            Formatter.NewLine();
        }

        private void EmitStringIntrinsics (int heapSize) {
            Formatter.WriteRaw(";; Compiler-generated string table routines");
            Formatter.NewLine();

            Formatter.WriteRaw("(func $__getStringFirstChar (param $offset i32) (result i32)");
            Formatter.NewLine();
            Formatter.Indent();

            Formatter.WriteRaw(
                "(i32.add (i32.const {0}) (get_local $offset))", 
                // Add 4 to skip past the length header
                heapSize + 4
            );
            Formatter.NewLine();

            Formatter.Unindent();
            Formatter.WriteRaw(")");
            Formatter.NewLine();
            Formatter.NewLine();

            Formatter.WriteRaw("(func $__getStringLength (param $firstCharAddress i32) (result i32)");
            Formatter.NewLine();
            Formatter.Indent();

            Formatter.WriteRaw(
                "(return (i32.load/i32/1 (i32.sub (get_local $firstCharAddress) (i32.const 4))))"
            );
            Formatter.NewLine();

            Formatter.Unindent();
            Formatter.WriteRaw(")");
            Formatter.NewLine();
            Formatter.NewLine();
        }

        public static void EmitStringLiteralContents (System.IO.TextWriter tw, IEnumerable<char> text) {
            foreach (var ch in text) {
                if ((ch < 32) || (ch >= 127)) {
                    tw.Write("\\{0:X2}", (byte)ch);
                } else {
                    tw.Write(ch);
                }
            }
        }

        private void EmitStringTable (int heapSize) {
            Formatter.Indent();
            Formatter.NewLine();

            Formatter.WriteRaw(";; string table");
            Formatter.NewLine();

            foreach (var kvp in StringTable.OrderBy(kvp => kvp.Value.Offset)) {
                Formatter.ConditionalNewLine();
                Formatter.WriteRaw(
                    "(segment {0} \"",
                    kvp.Value.Offset + heapSize
                );

                var lengthBytes = BitConverter.GetBytes(kvp.Value.Bytes.Length);
                foreach (var b in lengthBytes)
                    Formatter.WriteRaw("\\{0:X2}", b);

                EmitStringLiteralContents(Formatter.Output, (from b in kvp.Value.Bytes select (char)b));

                Formatter.WriteRaw("\")");
            }

            Formatter.Unindent();
            Formatter.ConditionalNewLine();
        }

        public void EmitFooter () {
            int heapSize = 0;
            if (Assembly.EntryPoint != null)
                WasmUtil.HeapSizes.TryGetValue(Assembly.EntryPoint.DeclaringType, out heapSize);

            int totalMemorySize = heapSize + StringTableSize;

            if (totalMemorySize > 0) {
                Switch(PrecedingType.Memory);

                if (StringTableSize > 0)
                    EmitStringIntrinsics(heapSize);
        
                Formatter.WriteRaw("(memory {0} {0}", totalMemorySize);

                if (StringTableSize > 0)
                    EmitStringTable(heapSize);

                Formatter.WriteRaw(")");
                Formatter.NewLine();
            }

            Formatter.Unindent();
            Formatter.NewLine();
            Formatter.WriteRaw(")\n");

            if (Assembly.EntryPoint != null) {
                Formatter.NewLine();
                EmitMain();
            }
        }

        public void EmitMain () {
            var typeId = new TypeIdentifier(Assembly.EntryPoint.DeclaringType);
            var memberId = new MemberIdentifier(Translator.TypeInfoProvider, Assembly.EntryPoint);
            var entryPointIdentifier = new QualifiedMemberIdentifier(typeId, memberId);

            var mainExpression = Translator.FunctionCache.GetExpression(entryPointIdentifier);

            var astEmitter = (AstEmitter)EntryPointAstEmitter;
            var mainEmitter = new AstEmitter(this, Formatter, astEmitter.JSIL, astEmitter.TypeSystem, astEmitter.TypeInfo, astEmitter.Configuration, isTopLevel: true);

            Switch(PrecedingType.TopLevel);

            var body = mainExpression.Body;
            foreach (var stmt in body.Statements) {
                mainEmitter.Visit(stmt);
                Formatter.ConditionalNewLine();
            }
        }

        public void EmitInterfaceList (TypeInfo typeInfo, IAstEmitter astEmitter, JSRawOutputIdentifier dollar) {
        }

        public void EmitMethodDefinition (DecompilerContext context, MethodReference methodRef, MethodDefinition method, IAstEmitter astEmitter, bool stubbed, JSRawOutputIdentifier dollar, MethodInfo methodInfo = null) {
            // Skip Main() and emit it at the footer
            if (Assembly.EntryPoint == method) {
                // HACK: Store this so we can use it to emit the entry point's body later
                EntryPointAstEmitter = astEmitter;
                return;
            }

            var name = WasmUtil.FormatMemberName(method);

            var sExport = "Wasm.Module.ExportAttribute";

            methodInfo = methodInfo ?? Translator.TypeInfoProvider.GetMethod(methodRef);

            if (methodInfo != null && methodInfo.Metadata.HasAttribute(sExport)) {
                var exportName = method.Name;
                var ap = methodInfo.Metadata.GetAttributeParameters(sExport);
                if ((ap.Count == 1) && (ap[0].Value is string))
                    exportName = (string)ap[0].Value;

                Switch(PrecedingType.Export);

                Formatter.WriteRaw("(export \"{0}\" ${1})", exportName, name); 
                Formatter.ConditionalNewLine();
            }
        }

        public void EmitPrimitiveDefinition (DecompilerContext context, TypeDefinition typedef, bool stubbed, JSRawOutputIdentifier dollar) {
        }

        public void EmitProperty (DecompilerContext context, IAstEmitter astEmitter, PropertyDefinition property, JSRawOutputIdentifier dollar) {
        }

        public void EmitField (DecompilerContext context, IAstEmitter astEmitter, FieldDefinition field, JSRawOutputIdentifier dollar, JSExpression defaultValue) {
            var fieldInfo = Translator.TypeInfoProvider.GetField(field);
            var typeKeyword = WasmUtil.PickTypeKeyword(fieldInfo.FieldType);

            // Unhandled type
            if (typeKeyword == null)
                return;

            Switch(PrecedingType.Global);

            Formatter.WriteRaw("(global ${0} {1})", WasmUtil.EscapeIdentifier(fieldInfo.Name), typeKeyword);
            Formatter.ConditionalNewLine();
        }

        public void EmitConstant (DecompilerContext context, IAstEmitter astEmitter, FieldDefinition field, JSRawOutputIdentifier dollar, JSExpression value) {
        }

        public void EmitProxyComment (string fullName) {
        }

        public void EmitSemicolon () {
        }

        public void EmitSpacer () {
        }

        public void EmitTypeAlias (TypeDefinition typedef) {
        }

        public bool EmitTypeDeclarationHeader (DecompilerContext context, IAstEmitter astEmitter, TypeDefinition typedef, TypeInfo typeInfo) {
            return true;
        }

        public void EndEmitTypeDefinition (IAstEmitter astEmitter, DecompilerContext context, TypeDefinition typedef) {
        }

        public IAstEmitter MakeAstEmitter (JSILIdentifier jsil, TypeSystem typeSystem, TypeInfoProvider typeInfoProvider, Configuration configuration) {
            return new AstEmitter(this, Formatter, jsil, typeSystem, typeInfoProvider, configuration, isTopLevel: false);
        }

        public void EmitCachedValues (IAstEmitter astEmitter, TypeExpressionCacher typeCacher, SignatureCacher signatureCacher, BaseMethodCacher baseMethodCacher) {
        }

        public void EmitFunctionBody (IAstEmitter astEmitter, MethodDefinition method, JSFunctionExpression function) {
            // Skip Main() and emit it at the footer
            if (Assembly.EntryPoint == method) {
                // HACK: Store this so we can use it to emit the entry point's body later
                EntryPointAstEmitter = astEmitter;
                return;
            }

            var name = WasmUtil.FormatMemberName(method);

            Switch(PrecedingType.Function, true);
            
            Formatter.WriteRaw("(func ${0}", name);
            Formatter.Indent();
            Formatter.NewLine();

            int v = 0;
            foreach (var kvp in function.AllVariables) {
                var variable = kvp.Value;

                var type = WasmUtil.PickTypeKeyword(variable.IdentifierType);
                if (type != null) {
                    Formatter.WriteRaw(
                        "({0} ${1} {2}) ",
                        variable.IsParameter 
                            ? "param" 
                            : "local",
                        WasmUtil.EscapeIdentifier(kvp.Key), type
                    );

                    if (v++ >= 3) {
                        v = 0;
                        Formatter.NewLine();
                    }
                }
            }

            if (function.LabelGroupCount > 0) {
                Formatter.NewLine();
            }
            for (var i = 0; i < function.LabelGroupCount; i++) {
                Formatter.WriteRaw("(local $currentLabel_{0} i32) ", i);
            }

            var returnType = WasmUtil.PickTypeKeyword(method.ReturnType);
            if (returnType != "void") {
                Formatter.NewLine();
                Formatter.WriteRaw("(result {0})", returnType);
            }

            Formatter.ConditionalNewLine();
            Formatter.NewLine();

            astEmitter.Emit(function.Body);

            Formatter.ConditionalNewLine();
            Formatter.Unindent();
            Formatter.WriteRaw(")");

            Formatter.NewLine();
        }
    }
}
