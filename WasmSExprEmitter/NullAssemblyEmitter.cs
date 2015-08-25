using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using JSIL;
using JSIL.Ast;
using JSIL.Compiler.Extensibility;
using JSIL.Internal;
using JSIL.Transforms;
using JSIL.Translator;
using Mono.Cecil;

namespace WasmSExprEmitter {
    public class NullAssemblyEmitter : IAssemblyEmitter {
        public void BeginEmitTypeDeclaration (TypeDefinition typedef) {
        }

        public void BeginEmitTypeDefinition (IAstEmitter astEmitter, TypeDefinition typedef, TypeInfo typeInfo, TypeReference baseClass) {
        }

        public void EmitAssemblyEntryPoint (AssemblyDefinition assembly, MethodDefinition entryMethod, MethodSignature signature) {
        }

        public void EmitCachedValues (IAstEmitter astEmitter, TypeExpressionCacher typeCacher, SignatureCacher signatureCacher, BaseMethodCacher baseMethodCacher) {
        }

        public void EmitCustomAttributes (DecompilerContext context, TypeReference declaringType, ICustomAttributeProvider member, IAstEmitter astEmitter, bool standalone = true) {
        }

        public void EmitEvent (DecompilerContext context, IAstEmitter astEmitter, EventDefinition @event, JSRawOutputIdentifier dollar) {
        }

        public void EmitField (DecompilerContext context, IAstEmitter astEmitter, FieldDefinition field, JSRawOutputIdentifier dollar, JSExpression defaultValue) {
        }

        public void EmitConstant (DecompilerContext context, IAstEmitter astEmitter, FieldDefinition field, JSRawOutputIdentifier dollar, JSExpression value) {
        }

        public void EmitFooter () {
        }

        public void EmitFunctionBody (IAstEmitter astEmitter, MethodDefinition method, JSFunctionExpression function) {
        }

        public void EmitHeader (bool stubbed) {
        }

        public void EmitInterfaceList (TypeInfo typeInfo, IAstEmitter astEmitter, JSRawOutputIdentifier dollar) {
        }

        public void EmitMethodDefinition (DecompilerContext context, MethodReference methodRef, MethodDefinition method, IAstEmitter astEmitter, bool stubbed, JSRawOutputIdentifier dollar, MethodInfo methodInfo = null) {
        }

        public void EmitPrimitiveDefinition (DecompilerContext context, TypeDefinition typedef, bool stubbed, JSRawOutputIdentifier dollar) {
        }

        public void EmitProperty (DecompilerContext context, IAstEmitter astEmitter, PropertyDefinition property, JSRawOutputIdentifier dollar) {
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
            return false;
        }

        public void EndEmitTypeDefinition (IAstEmitter astEmitter, DecompilerContext context, TypeDefinition typedef) {
        }

        public IAstEmitter MakeAstEmitter (JSILIdentifier jsil, TypeSystem typeSystem, TypeInfoProvider typeInfoProvider, Configuration configuration) {
            return new NullAstEmitter(typeSystem);
        }
    }
}
