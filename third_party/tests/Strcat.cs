using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    private static int strcat (int dest, string src) {
        for (int i = 0, l = src.Length; i < l; i++)
            U8[dest, i] = (byte)src[i];

        U8[dest, src.Length] = 0;
        return dest + src.Length;
    }

    [Export]
    public static void buildString () {
        int offset = strcat(0, "hello");
        offset = strcat(offset, ", ");
        offset = strcat(offset, "world");
        offset = strcat(offset, "!");

        SetStdout("strcat.log");
        Write(0, offset);
    }

    public static void Main () {
        SetHeapSize(1024);

        Invoke("buildString");
    }
}