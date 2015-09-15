using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSIL.Internal;
using Mono.Cecil;

namespace WasmSExprEmitter {
    public static class WasmUtil {
        public static readonly Dictionary<TypeDefinition, int> HeapSizes = new Dictionary<TypeDefinition, int>(new ReferenceComparer<TypeDefinition>());

        public static string FormatMemberName (IMemberDefinition member) {
            return EscapeIdentifier(
                member.DeclaringType.Name + "_" + member.Name
            );
        }

        public static string EscapeIdentifier (string identifier) {
            return Util.EscapeIdentifier(identifier).Replace("$", "_");
        }

        public static string PickTypeKeyword (TypeReference type) {
            if (type.IsPointer)
                return "i32";

            // FIXME
            switch (type.FullName) {
                case "System.Void":
                    return "void";

                case "System.Boolean":
                case "System.Int32":
                case "System.UInt32":
                    return "i32";

                case "System.Int64":
                case "System.UInt64":
                    return "i64";

                case "System.Single":
                    return "f32";
                
                case "System.Double":
                    return "f64";

                case "System.Byte":
                case "System.UInt16":
                case "System.SByte":
                case "System.Int16":
                    return "i32";

                case "System.Char":
                    return "i32";

                case "System.String":
                    // HACK: String table offset
                    return "i32";
            }

            return null;
        }

        public static string PickMemoryTypeSuffix (TypeReference type, bool isStore) {
            switch (type.FullName) {
                case "System.Byte":
                    if (isStore)
                        return "8";
                    else
                        return "8_u";

                case "System.SByte":
                    return "8";

                case "System.UInt16":
                    if (isStore)
                        return "16";
                    else
                        return "16_u";

                case "System.Int16":
                    return "16";

                // FIXME: i8 or i16 for char?
            }

            return "";
        }

        public static void ConditionalNewLine (this JavascriptFormatter formatter) {
            if (!formatter.PreviousWasLineBreak)
                formatter.NewLine();
        }

        public static void WriteSExpr (
            this JavascriptFormatter formatter, string keyword,
            Action<JavascriptFormatter> interior = null,
            bool lineBreakInside = false, bool lineBreakAfter = false
        ) {
            formatter.WriteRaw("({0} ", keyword);
            formatter.Indent();
            if (lineBreakInside)
                formatter.NewLine();

            if (interior != null)
                interior(formatter);

            if (lineBreakInside && !formatter.PreviousWasLineBreak)
                formatter.NewLine();
            formatter.Unindent();

            formatter.WriteRaw(")");
            if (lineBreakAfter)
                formatter.NewLine();
        }
    }
}
