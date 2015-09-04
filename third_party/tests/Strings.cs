using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    public static string getAString (int index) {
        switch (index) {
            case 1:
                return "hello";
            case 2:
                return "world!";
            case 3:
                return "\0\r\n\x07";
            default:
                return null;
        }
    }

    public static int strcmp (string lhs, string rhs) {
        if (lhs == null) {
            if (rhs == null)
                return 0;
            else
                return -1;
        } else if (rhs == null) {
            return 1;
        }

        var length = lhs.Length;
        if (rhs.Length < length)
            length = rhs.Length;

        for (var i = 0; i < length; i++) {
            var chL = lhs[i];
            var chR = rhs[i];

            if (chL < chR)
                return -1;
            else if (chL > chR)
                return 1;
        }

        // FIXME: Length difference
        return 0;
    }

    [Export]
    public static int compareStrings (int lhsIndex, int rhsIndex) {
        return strcmp(getAString(lhsIndex), getAString(rhsIndex));
    }

    [Export]
    public static int getStringLength (int stringIndex) {
        var str = getAString(stringIndex);
        return str.Length;
    }

    [Export]
    public static char readStringChar (int stringIndex, int offset) {
        var str = getAString(stringIndex);
        return str[offset];
    }

    public static void Main () {
        AssertEq(5,    "getStringLength", 1);
        AssertEq(6,    "getStringLength", 2);

        AssertEq('h',  "readStringChar",  1, 0);
        AssertEq('l',  "readStringChar",  1, 2);

        AssertEq('w',  "readStringChar",  2, 0);
        AssertEq('l',  "readStringChar",  2, 3);
        AssertEq('!',  "readStringChar",  2, 5);

        AssertEq(0,    "compareStrings",  0, 0);
        AssertEq(-1,   "compareStrings",  0, 1);
        AssertEq(1,    "compareStrings",  2, 0);
        AssertEq(0,    "compareStrings",  1, 1);
        AssertEq(-1,   "compareStrings",  1, 2);
        AssertEq(1,    "compareStrings",  2, 1);

        AssertEq('\0',   "readStringChar",  3, 0);
        AssertEq('\r',   "readStringChar",  3, 1);
        AssertEq('\n',   "readStringChar",  3, 2);
        AssertEq('\x07', "readStringChar",  3, 3);
    }
}