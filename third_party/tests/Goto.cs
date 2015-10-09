// Derived from third_party/JSIL/Tests/TestCases/Goto.cs
// Rewritten to run as wasm

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    public const int HeapSize = 1024;
    public const int MaxResultCount = 128;

    [Export]
    public static int ResultCount { get; set; }

    public const int _Result  = 0;

    public const byte A = 1;
    public const byte C = 3;

    private static void Clear (int offset, int count) {
        for (var i = 0; i < count; i++)
            U8[offset, i] = 0;
    }

    private static void AddResult (byte result) {
        var count = ResultCount;
        U8[_Result, count] = result;
        ResultCount = count + 1;
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
        ResultCount = 0;
        Clear(_Result, MaxResultCount);
        GotosInner();
    }

    [Export("getResult")]
    public static int GetResult (int index) {
        return (int)U8[_Result, index];
    }

    public static void Main () {
        SetHeapSize(HeapSize);

        Invoke("gotos");

        AssertReturn(16, "get_ResultCount");
        AssertReturn(A,  "getResult", 0);
        AssertReturn(C,  "getResult", 1);
        AssertReturn(C,  "getResult", 2);
        AssertReturn(C,  "getResult", 3);
        AssertReturn(C,  "getResult", 4);
        AssertReturn(C,  "getResult", 5);
        AssertReturn(C,  "getResult", 6);
        AssertReturn(C,  "getResult", 7);
        AssertReturn(A,  "getResult", 8);
        AssertReturn(C,  "getResult", 9);
        AssertReturn(C,  "getResult", 10);
        AssertReturn(C,  "getResult", 11);
        AssertReturn(C,  "getResult", 12);
        AssertReturn(C,  "getResult", 13);
        AssertReturn(C,  "getResult", 14);
        AssertReturn(C,  "getResult", 15);
        AssertReturn(0,  "getResult", 16);
   }
}