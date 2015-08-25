using System;
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
        private IAstEmitter EntryPointAstEmitter;

        public readonly AssemblyTranslator Translator;
        public readonly AssemblyDefinition Assembly;
        public readonly JavascriptFormatter Formatter;

        public WasmSExprAssemblyEmitter (AssemblyTranslator translator, AssemblyDefinition assembly, JavascriptFormatter formatter) {
            Translator = translator;
            Assembly = assembly;
            Formatter = formatter;
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

        public void EmitFooter () {
            if (Assembly.EntryPoint != null) {
                int heapSize;

                if (WasmUtil.HeapSizes.TryGetValue(Assembly.EntryPoint.DeclaringType, out heapSize)) {
                    Formatter.ConditionalNewLine();
                    Formatter.NewLine();
                    Formatter.WriteRaw("(memory {0} {0})", heapSize);
                    Formatter.NewLine();
                }
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
            var mainEmitter = new AstEmitter(Formatter, astEmitter.JSIL, astEmitter.TypeSystem, astEmitter.TypeInfo, astEmitter.Configuration, isTopLevel: true);

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

                Formatter.NewLine();
                Formatter.WriteRaw("(export \"{0}\" ${1})", exportName, name); 
                Formatter.NewLine();
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

            Formatter.WriteRaw("(global ${0} {1})", WasmUtil.FormatMemberName(field), typeKeyword);
            Formatter.NewLine();
            Formatter.NewLine();
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
            return new AstEmitter(Formatter, jsil, typeSystem, typeInfoProvider, configuration, isTopLevel: false);
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

            Formatter.ConditionalNewLine();
            Formatter.NewLine();
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
                        Util.EscapeIdentifier(kvp.Key), type
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

            Formatter.NewLine();
            Formatter.NewLine();

            astEmitter.Emit(function.Body);

            Formatter.NewLine();
            Formatter.Unindent();
            Formatter.WriteRaw(")");

            Formatter.NewLine();
        }
    }
}
