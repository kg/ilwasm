using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static partial class Stdout {
    public static int Length;

    public static void putchar (int ch) {
        U8[HeapBase, Length++] = (byte)ch;
    }

    public static void prints (string str) {
        for (int i = 0, l = str.Length; i < l; i++)
            putchar(str[i]);
    }

    public static void printi (int value) {
        const int zero = '0';

        int initial_offset = Length;
        bool negative = value < 0;

        if (negative)
            value = -value;

        // Output number in reverse
        for (; value > 0; value = value / 10)
            putchar(zero + (value % 10));

        if (negative)
            putchar('-');

        // In-place reverse result into correct order
        int j = (Length - initial_offset) - 1;
        int reverse_base = HeapBase + initial_offset;
        // Console.WriteLine("j={0} reverse_base={1}", j, reverse_base);
        for (int i = 0; i < j; i += 1, j -= 1) {
            byte temp = U8[reverse_base, i];
            U8[reverse_base, i] = U8[reverse_base, j];
            U8[reverse_base, j] = temp;
        }
    }

    [Export]
    static int get_stdout_length () {
        return Length;
    }
}