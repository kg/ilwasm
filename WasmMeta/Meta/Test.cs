using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JSIL.Meta;

namespace Wasm {
    [JSExternal]
    public static class Test {
        [JSIgnore]
        private class ExportTable {
            private static readonly Dictionary<Assembly, ExportTable> Cache = 
                new Dictionary<Assembly, ExportTable>();

            private readonly Dictionary<string, MethodInfo> Exports = 
                new Dictionary<string, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);

            private ExportTable (Assembly assembly) {
                foreach (var type in assembly.GetTypes()) {
                    var staticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in staticMethods) {
                        var exportAttr = method.GetCustomAttribute<Module.ExportAttribute>();
                        if (exportAttr == null)
                            continue;

                        var name = exportAttr.Name ?? method.Name;
                        Exports.Add(name, method);
                    }
                }
            }

            public MethodInfo this [string name] {
                get {
                    MethodInfo result;
                    if (!Exports.TryGetValue(name, out result))
                        throw new ArgumentException("No export named '" + name + "'", "name");

                    return result;
                }
            }

            public static ExportTable GetExports (Assembly assembly) {
                ExportTable result;

                if (!Cache.TryGetValue(assembly, out result))
                    Cache.Add(assembly, result = new ExportTable(assembly));

                return result;
            }
        }

        [JSIsPure]
        public static void AssertEq (object expected, string exportedFunctionName, params object[] values) {
            var assembly = Assembly.GetCallingAssembly();
            var exports = ExportTable.GetExports(assembly);
            var export = exports[exportedFunctionName];
            var result = export.Invoke(null, values);
            var expectedText = Convert.ToString(expected);
            var actualText   = Convert.ToString(result);

            Console.WriteLine(
                "(invoke \"{1}\" {2}){0}" +
                "-> {3} '{4}' == '{5}'",
                Environment.NewLine,
                exportedFunctionName,
                string.Join(" ", values),                
                (expectedText == actualText)
                    ? "pass"
                    : "fail",
                expectedText,
                actualText
            );
        }

        [JSIsPure]
        public static void Printf (string format, params object[] values) {
            Console.WriteLine(format, values);
        }

        [JSIsPure]
        public static void Invoke (string exportedFunctionName, params object[] values) {
            var assembly = Assembly.GetCallingAssembly();
            var exports = ExportTable.GetExports(assembly);
            var export = exports[exportedFunctionName];
            var result = export.Invoke(null, values);

            Console.WriteLine(
                "(invoke \"{1}\" {2}){0}" +
                "-> {3}",
                Environment.NewLine,
                exportedFunctionName,
                string.Join(" ", values),
                result
            );
        }
    }
}
