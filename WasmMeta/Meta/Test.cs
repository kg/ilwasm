using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using JSIL.Meta;

namespace Wasm {
    [JSExternal]
    public static class Test {
        public static readonly bool QuietMode;

        private static bool HeaderPrinted;

        static Test () {
            var args = Environment.GetCommandLineArgs();
            QuietMode = args.Contains("--quiet");
        }

        [JSIgnore]
        private class ExportTable {
            private static readonly Dictionary<Assembly, ExportTable> Cache = 
                new Dictionary<Assembly, ExportTable>();

            private readonly Dictionary<string, MethodInfo> Exports = 
                new Dictionary<string, MethodInfo>(StringComparer.InvariantCultureIgnoreCase);

            private ExportTable (Assembly assembly) {
                var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                foreach (var type in assembly.GetTypes()) {
                    var staticMethods    = type.GetMethods(flags);
                    var staticProperties = type.GetProperties(flags);

                    foreach (var prop in staticProperties) {
                        var exportAttr = prop.GetCustomAttribute<Module.ExportAttribute>();
                        if (exportAttr == null)
                            continue;

                        var name = exportAttr.Name ?? prop.Name;

                        if (prop.GetMethod != null)
                            Exports.Add("get_" + name, prop.GetMethod);
                        if (prop.SetMethod != null)
                            Exports.Add("set_" + name, prop.SetMethod);
                    }

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

        private static void PrintHeader (Assembly assembly) {
            if (HeaderPrinted)
                return;

            HeaderPrinted = true;
            var testName = System.IO.Path.GetFileNameWithoutExtension(assembly.Location);
            Console.WriteLine("// {0}", testName);
        }

        public static void AssertEq (object expected, string exportedFunctionName, params object[] values) {
            var assembly = Assembly.GetCallingAssembly();
            var exports = ExportTable.GetExports(assembly);
            var export = exports[exportedFunctionName];
            var result = export.Invoke(null, values);
            var expectedText = Convert.ToString(expected);
            var actualText   = Convert.ToString(result);

            var passed = (expectedText == actualText);
            if (!passed)
                Environment.ExitCode = 1;

            if (QuietMode && passed)
                return;

            PrintHeader(assembly);
            Console.WriteLine(
                "(invoke \"{1}\" {2}){0}" +
                "-> {3} '{4}' == '{5}'",
                Environment.NewLine,
                exportedFunctionName,
                string.Join(" ", values),                
                passed
                    ? "pass"
                    : "fail",
                expectedText,
                actualText
            );
        }

        private static IEnumerable<byte> RawCharsToBytes (IEnumerable<char> chars) {
            return (from ch in chars select (byte)ch);
        }

        private static string FormatBytestring (IEnumerable<byte> bytes) {
            var sb = new StringBuilder();
            foreach (var b in bytes) {
                if ((b < 32) || (b >= 127)) {
                    sb.AppendFormat("\\x{0:X2}", (int)b);
                } else {
                    sb.Append((char)b);
                }
            }

            return sb.ToString();
        }

        private static IEnumerable<byte> GetHeapRange (int offset, int count) {
            var indices = Enumerable.Range(0, count);
            var bytes = (from i in indices select Wasm.Heap.U8[offset, i]);
            return bytes;
        }

        private static string FormatHeapRange (int offset, int count) {
            return FormatBytestring(GetHeapRange(offset, count));
        }

        private static bool TestHeapEq (int offset, IEnumerable<byte> expected) {
            int i = 0;
            foreach (var expectedByte in expected) {
                byte b = Wasm.Heap.U8[offset, i];
                if (b != expectedByte)
                    return false;

                i++;
            }

            return true;
        }

        public static void AssertHeapEq (int offset, string expected) {
            var assembly = Assembly.GetCallingAssembly();
            var expectedBytes = RawCharsToBytes(expected);

            var passed = TestHeapEq(offset, expectedBytes);

            if (QuietMode && passed)
                return;

            PrintHeader(assembly);
            Console.WriteLine(
                "(assert_heap_eq {1} \"{2}\"){0}" +
                "-> {3} \"{2}\" == \"{4}\"",
                Environment.NewLine,
                offset, FormatBytestring(expectedBytes),
                passed
                    ? "pass"
                    : "fail",
                FormatHeapRange(offset, expectedBytes.Count())
            );
        }

        public static void AssertHeapEqFile (int offset, int count, string expectedFileName) {
            var assembly = Assembly.GetCallingAssembly();

            var actualBytes = GetHeapRange(offset, count);
            var actualFileName = Path.Combine("output", expectedFileName);
            File.WriteAllBytes(actualFileName, actualBytes.ToArray());

            var fullExpectedFileName = Path.Combine("third_party", "test_data", expectedFileName);

            var expectedBytes = File.ReadAllBytes(fullExpectedFileName);

            var passed = TestHeapEq(offset, expectedBytes) && (count == expectedBytes.Length);

            if (QuietMode && passed)
                return;

            PrintHeader(assembly);
            Console.WriteLine(
                "(assert_heap_eq {1} '{2}'){0}" +
                "-> {3} '{4}' == '{5}'",
                Environment.NewLine,
                offset, expectedFileName,
                passed
                    ? "pass"
                    : "fail",
                fullExpectedFileName,
                actualFileName
            );
        }

        public static void Invoke (string exportedFunctionName, params object[] values) {
            var assembly = Assembly.GetCallingAssembly();
            var exports = ExportTable.GetExports(assembly);
            var export = exports[exportedFunctionName];
            var result = export.Invoke(null, values);

            if (QuietMode && (export.ReturnType.FullName == "System.Void"))
                return;

            PrintHeader(assembly);
            if (export.ReturnType.Name != "Void")
                Console.WriteLine(
                    "(invoke \"{1}\" {2}){0}" +
                    "-> {3}",
                    Environment.NewLine,
                    exportedFunctionName,
                    string.Join(" ", values),
                    result
                );
            else
                Console.WriteLine(
                    "(invoke \"{0}\" {1})",
                    exportedFunctionName,
                    string.Join(" ", values)
                );
        }
    }
}
