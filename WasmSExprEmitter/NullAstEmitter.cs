using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSIL.Ast;
using JSIL.Compiler.Extensibility;
using JSIL.Internal;
using JSIL.Transforms;
using Mono.Cecil;

namespace WasmSExprEmitter {
    public class NullAstEmitter : JSAstVisitor, IAstEmitter {
        public readonly TypeReferenceContext ReferenceContext = new TypeReferenceContext();
        public readonly TypeSystem TypeSystem;

        public NullAstEmitter (TypeSystem typeSystem) {
            TypeSystem = typeSystem;
        }

        public SignatureCacher SignatureCacher {
            get; set;
        }

        TypeSystem IAstEmitter.TypeSystem {
            get {
                return TypeSystem;
            }
        }

        TypeReferenceContext IAstEmitter.ReferenceContext {
            get {
                return ReferenceContext;
            }
        }

        public void Emit (JSNode node) {
        }
    }
}
