using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSIL;
using JSIL.Internal;
using Mono.Cecil;

namespace WasmSExprEmitter {
    public static class WasmUtil {
        public static readonly Dictionary<TypeDefinition, int> HeapSizes = new Dictionary<TypeDefinition, int>(new ReferenceComparer<TypeDefinition>());

        public static string FormatMemberName (IMemberDefinition member) {
            var typePrefix = member.DeclaringType.Name + "_";

            if (member.Name.Contains(typePrefix))
                return EscapeIdentifier(member.Name);
            else
                return EscapeIdentifier(typePrefix + member.Name);
        }

        public static string EscapeIdentifier (string identifier) {
            return Util.EscapeIdentifier(identifier).Replace("$", "_");
        }

        public static string PickTypeKeyword (TypeReference type) {
            if (type.IsPointer)
                return "i32";
            else if (TypeUtil.IsDelegateType(type))
                return "i32";

            // FIXME
            switch (type.FullName) {
                case "System.Void":
                    return "void";

                case "System.Boolean":
                case "System.Int32":
                case "System.UInt32":
                case "JSIL.Types.NativeInt":
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

        public static int OffsetOfField (FieldDefinition fd) {
            int result = 0;
            var td = fd.DeclaringType.Resolve();

            foreach (var _fd in td.Fields) {
                if (fd == _fd)
                    return result;

                result += SizeOfType(_fd.FieldType);
            }

            throw new Exception("Error computing field offset");
        }

        public static int SizeOfStruct (TypeReference type) {
            int result = 0;
            var td = type.Resolve();

            foreach (var fd in td.Fields)
                result += SizeOfType(fd.FieldType);

            return result;
        }

        public static int SizeOfType (TypeReference type) {
            if (type.IsPointer)
                return 4;
            else if (TypeUtil.IsDelegateType(type))
                return 4;

            if (type.IsValueType && !type.IsPrimitive)
                return SizeOfStruct(type);

            return TypeUtil.SizeOfType(type);
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
