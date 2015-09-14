// see llvm_license.txt
// https://github.com/llvm-mirror/test-suite/blob/master/SingleSource/Benchmarks/BenchmarkGame/puzzle.c

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

using iptr = System.Int32;

public static class Program {
    const int   ARRAY_SIZE = 5000;
    const int   NLOOPS1    = 10;

    const int   RAND_MAX          = 32767;
    const int   RAND_MAX_PLUSONE  = RAND_MAX + 1;
    const float RAND_MAX_PLUSONEF = RAND_MAX + 1.0f;

    const int   INT_SIZE   = 4;

    const int   STDOUT          = 40960;
    const int   STDOUT_CAPACITY = 1024;

    static long next = 1;

    [Export]
    public static int  stdout_length { get; set; }

    // RNG implemented localy to avoid library incongruences
    static int rand () {
        next = (next * 1103515245) + 12345;
        long divided = next >> 16;
        return (int)(((uint)divided) % RAND_MAX_PLUSONE);
    }

    static void srand (uint seed) {
        next = seed;
    }

    static int randInt (int min, int max) {
        int k, n;
        n = (max - min) + 1;
        float randval = rand();
        randval = randval / RAND_MAX_PLUSONEF;
        k = (int)(n * randval);
        return (k == n) ? k + min - 1 : k + min;
    }

    static void shuffle (iptr items, int len) {
        int j, k, i;
        int aux;

        for (i = len-1; i > 0; --i) {
            float randval = rand();
            randval = randval / RAND_MAX_PLUSONEF;
            k = (int)((i + 1) * randval);
            j = (k == (i + 1)) ? k - 1 : k;

            aux = I32[items, i];
            I32[items, i] = I32[items, j];
            I32[items, j] = aux;
        }
    }

    static iptr createRandomArray (int size) {
        int i, len;
        iptr result;

        len = size + 1;
        result = imalloc(len);

        for (i = 0; i < len; i++)
            I32[result, i] = i;

        I32[result, 0] = randInt(1, size);

        shuffle(result, len);
        return result;
    }

    static int findDuplicate (iptr data, int len) {
        int i;
        int result = 0;

        for (i = 0; i < len; i++)
            result = result ^ (i + 1) ^ I32[data, i];

        result ^= len;
        return result;
    }

    [Export]
    static void test () {
        int i, j, duplicate = 0;
        iptr rndArr;

        srand(1);

        for (i = 0; i < NLOOPS1; i++) {
            rndArr = createRandomArray(ARRAY_SIZE);
            duplicate = findDuplicate(rndArr, ARRAY_SIZE+1);
            ifree(rndArr);

            prints("Found duplicate: ");
            printi(duplicate);
            prints("\n");
        }
    }

    static iptr imalloc (int size) {
        // HACK: always put data at 0
        return 0;
    }

    static void ifree (iptr ptr) {
        // HACK: no-op
    }

    static void prints (string str) {
        int l = str.Length;
        for (int i = 0; i < l; i++) {
            U8[STDOUT, stdout_length] = (byte)str[i];
            stdout_length = stdout_length + 1;
        }
    }

    static void printi (int value) {
        const byte zero = (byte)'0';

        int initial_offset = stdout_length;
        bool negative = value < 0;

        if (negative)
            value = -value;

        // Output number in reverse
        for (int i = 0; value > 0; value = value / 10) {
            var digit = value % 10;
            U8[STDOUT, stdout_length] = (byte)(zero + digit);
            stdout_length = stdout_length + 1;
        }

        if (negative) {
            U8[STDOUT, stdout_length] = (byte)'-';
            stdout_length = stdout_length + 1;
        }

        // In-place reverse result into correct order
        int j = (stdout_length - initial_offset) - 1;
        int reverse_base = STDOUT + initial_offset;
        for (int i = 0; i < j; i += 1, j -= 1) {
            byte temp = U8[reverse_base, i];
            U8[reverse_base, i] = U8[reverse_base, j];
            U8[reverse_base, j] = temp;
        }
    }

    static void Main () {
        SetHeapSize(STDOUT + STDOUT_CAPACITY);

        Invoke("test");

        const int expectedLength = 219;

        AssertEq(expectedLength, "get_stdout_length");
        AssertHeapEqFile(STDOUT, expectedLength, "puzzle.log");
    }

}