using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSIL;
using JSIL.Compiler.Extensibility;
using JSIL.Internal;
using Mono.Cecil;

namespace WasmSExprEmitter {
    public class Analyzer : IAnalyzer  {
        private readonly HashSet<string> PreviousWarnings = new HashSet<string>();

        public string SettingsKey {
            get {
                return "WasmSExpr";
            }
        }

        public void Analyze (AssemblyTranslator translator, AssemblyDefinition[] assemblies, TypeInfoProvider typeInfoProvider) {
        }

        public void SetConfiguration (IDictionary<string, object> analyzerSettings) {
        }

        private static Tuple<bool, string> Skip(bool cond, string reason = null) {
            return new Tuple<bool, string>(cond, reason);
        }

        public static Tuple<bool, string> ShouldSkipMember (MemberReference member) {
            var fr = member as FieldReference;
            var mr = member as MethodReference;

            if (fr != null)
                return Skip(WasmUtil.PickTypeKeyword(fr.FieldType) == null, "Unsupported field type");

            if (mr != null)
                return Skip(WasmUtil.PickTypeKeyword(mr.ReturnType) == null, "Unsupported return type");

            return Skip(false);            
        }

        private bool ShouldSilence (AssemblyTranslator translator, MemberReference member) {
            var typeInfo = translator.TypeInfoProvider.GetTypeInformation(member.DeclaringType);
            if (typeInfo.IsIgnored || typeInfo.IsExternal)
                return true;

            var asm = member.DeclaringType.Module.Assembly;
            if (translator.IsStubbed(asm) || translator.IsIgnored(asm))
                return true;

            return false;
        }

        private void Warn (AssemblyTranslator translator, MemberReference member, string reason) {
            if (ShouldSilence(translator, member))
                return;

            if (PreviousWarnings.Contains(member.FullName))
                return;

            PreviousWarnings.Add(member.FullName);
            Console.WriteLine("// Skipping {0}: {1}", member.Name, reason);
        }

        bool IAnalyzer.ShouldSkipMember (AssemblyTranslator translator, MemberReference member) {
            var result = ShouldSkipMember(member);

            if (result.Item1)
                Warn(translator, member, result.Item2);

            return result.Item1;
        }

        public IEnumerable<IFunctionTransformer> FunctionTransformers {
            get {
                yield return new FunctionTransformer();
            }
        }
    }
}
