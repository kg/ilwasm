using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    [Export]
    public static void countUp (int outOffset, int count) {
        for (int i = 0; i < count; i++)
            I32[outOffset, i] = i;
    }

    [Export]
    public static int readI32 (int @base, int offset) {
        return I32[@base, offset];
    }

    public static void Main () {
        SetHeapSize(4096);

        Invoke("countUp", 0, 32);
        Invoke("countUp", 16, 4);

        AssertEq(0,  "readI32", 0, 0 );
        AssertEq(2,  "readI32", 0, 2 );
        AssertEq(31, "readI32", 0, 31);
        AssertEq(0,  "readI32", 16, 0 );
        AssertEq(3,  "readI32", 16, 3 );
    }
}