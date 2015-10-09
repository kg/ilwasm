//#use lib/Stdout.cs
//#stdout puzzle.log

// see llvm_license.txt
// https://github.com/llvm-mirror/test-suite/blob/master/SingleSource/Benchmarks/BenchmarkGame/puzzle.c

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;
using static Stdout;

public static partial class Stdout {
    public const int HeapBase     = 20480;
    public const int HeapCapacity = 2048;
}

public static unsafe class Program {
    const int   ARRAY_SIZE = 5000;
    const int   NLOOPS1    = 10;

    const int   RAND_MAX          = 32767;
    const float RAND_MAX_PLUSONEF = RAND_MAX + 1.0f;

    const int   INT_SIZE   = 4;

    static ulong next = 1;

    // RNG implemented localy to avoid library incongruences
    static int rand () {
        next = next * 1103515245 + 12345;
        return (int)((uint)(next / 65536) % RAND_MAX+1);
    }

    static void srand (uint seed) {
        next = seed;
    }

    static int randInt (int min, int max) {
        int n = (max - min) + 1;
        float randval = rand() / RAND_MAX_PLUSONEF;
        int k = (int)(n * randval);
        return (k == n) ? k + min - 1 : k + min;
    }

    static void shuffle (int* items, int len) {
        for (int i = len-1; i > 0; --i) {
            float randval = rand() / RAND_MAX_PLUSONEF;
            int k = (int)((i + 1) * randval);
            int j = (k == (i + 1)) ? k - 1 : k;

            int aux = items[i];
            items[i] = items[j];
            items[j] = aux;
        }
    }

    static int* createRandomArray (int size) {
        int len = size + 1;
        int* result = imalloc(len);

        for (int i = 0; i < len; i++)
            result[i] = i;

        result[0] = randInt(1, size);

        shuffle(result, len);
        return result;
    }

    static int findDuplicate (int* data, int len) {
        int result = 0;

        for (int i = 0; i < len; i++)
            result = result ^ (i + 1) ^ data[i];

        result ^= len;
        return result;
    }

    [Export]
    static void test () {
        int duplicate = 0;
        int* rndArr;

        srand(1);

        for (int i = 0; i < NLOOPS1; i++) {
            rndArr = createRandomArray(ARRAY_SIZE);
            duplicate = findDuplicate(rndArr, ARRAY_SIZE+1);
            ifree(rndArr);

            prints("Found duplicate: ");
            printi(duplicate);
            prints("\n");
        }
    }

    static int* imalloc (int size) {
        // HACK: always put data at 0
        return I32.Base;
    }

    static void ifree (int* ptr) {
        // HACK: no-op
    }

    static void Main () {
        SetHeapSize(Stdout.HeapBase + Stdout.HeapCapacity);

        Invoke("test");

        const int expectedLength = 219;

        AssertReturn(expectedLength, "get_stdout_length");
        Write(Stdout.HeapBase, expectedLength);
    }

}