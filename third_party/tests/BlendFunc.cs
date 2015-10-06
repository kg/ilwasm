//#use lib/Targa.cs

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static unsafe class Program {
    public delegate void BlendFunc (byte* src, byte* dest, int count);

    public const int width = 16;
    public const int height = 16;
    public const int numPixels = (width * height);
    public const int numBytes  = numPixels * 3;

    public static void blend50 (byte* src, byte* dest, int count) {
        while (count-- > 0) {
            var r = (byte)((src[0] + dest[0]) / 2);
            var g = (byte)((src[1] + dest[1]) / 2);
            var b = (byte)((src[2] + dest[2]) / 2);

            dest[0] = r;
            dest[1] = g;
            dest[2] = b;

            src += 3;
            dest += 3;
        }
    }

    public static void checkerboard (byte* dest, int count, byte initial) {
        var v = initial;

        while (count-- > 0) {
            *dest++ = (byte)(count % 255);
            *dest++ = v;
            *dest++ = v;

            v = (byte)(v ^ 0xFF);
        }
    }

    public static void rasterize (BlendFunc bf, byte* src, byte* dest) {
        checkerboard(src,  numPixels, 0x0);
        checkerboard(dest, numPixels, 0xFF);

        var rowSize = width * 3;

        for (var y = 0; y < height; y++) {
            bf(src, dest, width);
            src += rowSize;
            dest += rowSize;
        }
    }

    [Export]
    public static int test () {
        var offset = Targa.EmitHeader(0, width, height);

        byte* dest = &U8.Base[offset];
        byte* src  = &dest[numBytes];

        rasterize(blend50, src, dest);

        return offset + numBytes;
    }

    public static void Main () {
        SetHeapSize(8192);

        const int expectedLength = 786;
        AssertEq(expectedLength, "test");
        AssertHeapEqFile(0, expectedLength, "blendfunc.tga");
    }
}