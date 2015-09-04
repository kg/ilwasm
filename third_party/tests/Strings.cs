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
                return "world";
            default:
                return null;
        }
    }

    // FIXME: How do you encode raw bytes into wasm memory segments? :/
    /*
    [Export]
    public static int getStringLength (int stringIndex) {
        var str = getAString(stringIndex);
        return str.Length;
    }
    */

    [Export]
    public static char readStringChar (int stringIndex, int offset) {
        var str = getAString(stringIndex);
        return str[offset];
    }

    public static void Main () {
        SetHeapSize(256);

        AssertEq('h', "readStringChar", 1, 0);
        AssertEq('l', "readStringChar", 1, 2);
        AssertEq('w', "readStringChar", 2, 0);
        AssertEq('l', "readStringChar", 2, 3);
    }
}