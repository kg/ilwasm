// Derived from third_party/JSIL/Tests/TestCases/Goto.cs
// Rewritten to run as wasm

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    public const int HeapSize = 1024;
    public const int MaxResultCount = 128;

    public const int _ResultCount = 0;
    public const int _Result  = 4;

    public const byte A = 1;
    public const byte C = 3;

    private static void Clear (int offset, int count) {
        for (var i = 0; i < count; i++)
            U8[offset, i] = 0;
    }

    private static void AddResult (byte result) {
        var count = I32[_ResultCount];
        U8[_Result, count] = result;
        I32[_ResultCount] = count + 1;
    }

    private static void GotosInner () {
        int i = 0;
        a:
            i += 1;
            AddResult(A);
        
        for (; i < 16; i++) {
            if (i == 8)
                goto a;
            else
                goto c;
            
            c:
                AddResult(C);
        }
    }

    [Export("gotos")]
    public static void Gotos () {
        I32[_ResultCount] = 0;
        Clear(_Result, MaxResultCount);
        GotosInner();
    }

    [Export("getResultCount")]
    public static int GetResultCount () {
        return I32[_ResultCount];
    }

    [Export("getResult")]
    public static int GetResult (int index) {
        return (int)U8[_Result, index];
    }

    public static void Main () {
        SetHeapSize(HeapSize);

        Invoke("gotos");

        AssertEq(16, "getResultCount");
        AssertEq(A,  "getResult", 0);
        AssertEq(C,  "getResult", 1);
        AssertEq(C,  "getResult", 2);
        AssertEq(C,  "getResult", 3);
        AssertEq(C,  "getResult", 4);
        AssertEq(C,  "getResult", 5);
        AssertEq(C,  "getResult", 6);
        AssertEq(C,  "getResult", 7);
        AssertEq(A,  "getResult", 8);
        AssertEq(C,  "getResult", 9);
        AssertEq(C,  "getResult", 10);
        AssertEq(C,  "getResult", 11);
        AssertEq(C,  "getResult", 12);
        AssertEq(C,  "getResult", 13);
        AssertEq(C,  "getResult", 14);
        AssertEq(C,  "getResult", 15);
        AssertEq(0,  "getResult", 16);
    }
}