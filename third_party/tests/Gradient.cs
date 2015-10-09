//#use lib/Targa.cs

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    [Export]
    public static int generateGradient (int heapOffset) {
        const int width = 8;
        const int height = 8;
        const int xScale = 256 / width;
        const int yScale = 256 / height;
        const int bytesPerPixel = Targa.BPP / 8;

        heapOffset = Targa.EmitHeader(heapOffset, width, height);

        for (int y = 0; y < height; y++) {
            int rowOffset = heapOffset + ((width * bytesPerPixel) * y);

            for (int x = 0; x < width; x++) {
                int pixelOffset = rowOffset + (x * bytesPerPixel);

                U8[pixelOffset, 0] = (byte)(y * yScale);
                U8[pixelOffset, 1] = 0;
                U8[pixelOffset, 2] = (byte)(x * xScale);
            }
        }

        const int length = (width * height * bytesPerPixel);
        SetStdout("gradient.tga");
        Write(heapOffset, length);

        return heapOffset + length;
    }

    public static void Main () {
        SetHeapSize(2048);

        const int expectedLength = 210;
        AssertReturn(expectedLength, "generateGradient", 0);
    }
}