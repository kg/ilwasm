// Derived from third_party/JSIL/Tests/PerformanceTestCases/Sieve.cs
// Rewritten to run as wasm

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    public const int HeapSize = (1024 * 128);
    public const int MaxResultCount = 4096;

    public const int _ResultCount = 0;
    public const int _Result  = 4;
    public const int _Scratch = ((MaxResultCount * 4) + 4);

    private static void Clear (int offset, int count) {
        for (var i = 0; i < count; i++)
            U8[offset, i] = 0;
    }

    private static void AddResult (int result) {
        var count = I32[_ResultCount];
        I32[_Result, count] = result;        
        I32[_ResultCount] = count + 1;
    }

    [Export("sieve")]
    public static void Sieve (int target) {
        I32[_ResultCount] = 0;
        Clear(_Result, MaxResultCount * 4);
        Clear(_Scratch, target);

        var squareRoot = Math.Sqrt(target);

        AddResult(2);

        for (int candidate = 3; candidate < target; candidate += 2) {
            if (U8[_Scratch, candidate] != 0)
                continue;

            if (candidate < squareRoot) {
                for (int multiple = candidate * candidate; multiple < target; multiple += 2 * candidate)
                    U8[_Scratch, multiple] = 1;
            }

            AddResult(candidate);
        }
    }

    [Export("getResultCount")]
    public static int GetResultCount () {
        return I32[_ResultCount];
    }

    [Export("getResult")]
    public static int GetResult (int index) {
        return I32[_Result, index];
    }

    public static void Main () {
        SetHeapSize(HeapSize);

        Invoke("sieve", 24);

        AssertEq(9, "getResultCount");
        AssertEq(2,  "getResult", 0);
        AssertEq(3,  "getResult", 1);
        AssertEq(5,  "getResult", 2);
        AssertEq(7,  "getResult", 3);
        AssertEq(11, "getResult", 4);
        AssertEq(13, "getResult", 5);
        AssertEq(17, "getResult", 6);
        AssertEq(19, "getResult", 7);
        AssertEq(23, "getResult", 8);
    }
}