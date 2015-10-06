using System;
using Wasm.Module;

using static Wasm.Heap;

public static partial class Targa {
    public const int BPP = 24;

    [Export("emitTargaHeader")]
    public static unsafe int EmitHeader (int offset, int width, int height) {
        var p = &U8.Base[offset];

        // ID length
        *p++ = 0;

        // Colormap type - none
        *p++ = 0;

        // Image type - uncompressed truecolor
        *p++ = 2;

        // Colormap specification
        for (var j = 0; j < 5; j++)
            *p++ = 0;

        // Image specification

        // X origin
        *p++ = 0;
        *p++ = 0;

        // Y origin
        *p++ = 0;
        *p++ = 0;

        // Width
        *p++ = (byte)width;
        width = width >> 8;
        *p++ = (byte)width;

        // Height
        *p++ = (byte)height;
        height = height >> 8;
        *p++ = (byte)height;

        // Bits per pixel
        *p++ = (byte)BPP;

        // Image descriptor [0..3] = alpha depth, [4..5] direction
        *p++ = 0;

        return (int)(p - U8.Base);
    }
}