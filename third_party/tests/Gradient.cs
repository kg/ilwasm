using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    const int BPP = 24;

    private static int EmitTargaHeader (int offset, int width, int height) {
        int i = 0;
        // ID length
        U8[offset, i++] = 0;

        // Colormap type - none
        U8[offset, i++] = 0;

        // Image type - uncompressed truecolor
        U8[offset, i++] = 2;

        // Colormap specification
        for (var j = 0; j < 5; j++)
            U8[offset, i++] = 0;

        // Image specification

        // X origin
        U8[offset, i++] = 0;
        U8[offset, i++] = 0;

        // Y origin
        U8[offset, i++] = 0;
        U8[offset, i++] = 0;

        // Width
        U8[offset, i++] = (byte)width;
        width = width >> 8;
        U8[offset, i++] = (byte)width;

        // Height
        U8[offset, i++] = (byte)height;
        height = height >> 8;
        U8[offset, i++] = (byte)height;

        // Bits per pixel
        U8[offset, i++] = BPP;

        // Image descriptor [0..3] = alpha depth, [4..5] direction
        U8[offset, i++] = 0;

        return offset + i;
    }

    [Export]
    public static int generateGradient (int heapOffset) {
        const int width = 8;
        const int height = 8;
        const int xScale = 256 / width;
        const int yScale = 256 / height;
        const int bytesPerPixel = BPP / 8;

        heapOffset = EmitTargaHeader(heapOffset, width, height);

        for (int y = 0; y < height; y++) {
            int rowOffset = heapOffset + ((width * bytesPerPixel) * y);

            for (int x = 0; x < width; x++) {
                int pixelOffset = rowOffset + (x * bytesPerPixel);

                U8[pixelOffset, 0] = (byte)(y * yScale);
                U8[pixelOffset, 1] = 0;
                U8[pixelOffset, 2] = (byte)(x * xScale);
            }
        }

        return heapOffset + (width * height * bytesPerPixel);
    }

    public static void Main () {
        SetHeapSize(2048);

        const int expectedLength = 210;
        AssertEq(expectedLength, "generateGradient", 0);
        AssertHeapEqFile(0, expectedLength, "gradient.tga");
    }
}