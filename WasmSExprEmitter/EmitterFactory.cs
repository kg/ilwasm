using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JSIL;
using JSIL.Compiler.Extensibility;
using JSIL.Internal;
using Mono.Cecil;
using Configuration = JSIL.Translator.Configuration;

namespace WasmSExprEmitter {
    public class WasmSExprEmitterFactory : IEmitterFactory {
        static WasmSExprEmitterFactory () {
            // HACK: Make sure our custom node types work
            JSIL.Ast.JSNode.NodeAssemblies.Add(Assembly.GetExecutingAssembly());
        }

        public string FileExtension {
            get {
                return ".sexpr";
            }
        }

        public IAssemblyEmitter MakeAssemblyEmitter (AssemblyTranslator translator, AssemblyDefinition assembly, JavascriptFormatter formatter) {
            if (translator.IsStubbed(assembly) || translator.IsIgnored(assembly))
                return new NullAssemblyEmitter();
            else
                return new WasmSExprAssemblyEmitter(translator, assembly, formatter);
        }

        public IEnumerable<IAnalyzer> GetAnalyzers () {
            yield return new Analyzer();
        }

        public Configuration FilterConfiguration (Configuration configuration) {
            var result = configuration.Clone();

            // The default proxies contain JS-specific replacements/hacks that aren't necessarily correct for wasm
            result.UseDefaultProxies = false;
            result.IncludeDependencies = false;

            result.CodeGenerator.IntroduceCharCasts = false;
            result.CodeGenerator.IntroduceEnumCasts = false;
            result.CodeGenerator.EmulateInt64       = false;

            return result;
        }
    }
}
