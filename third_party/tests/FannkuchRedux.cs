/* 
  The Computer Language Benchmarks Game
  http://shootout.alioth.debian.org/
  contributed by Isaac Gouy, transliterated from Mike Pall's Lua program 
*/

// Derived from third_party/JSIL/Tests/TestCases/FannkuchRedux.cs
// Rewritten to run as wasm

using System;
using Wasm.Module;

using static Wasm.Heap;
using static Wasm.Test;

public static class Program {
    const int _Result = 0;
    const int _P = 32;
    const int _Q = 32 * 2;
    const int _S = 32 * 3;

    static int maxflips, sum;

    private static void initHeap (int n) {
        for (int i = 0; i < n; i++) {
            I32[_P, i] = i;
            I32[_Q, i] = i;
            I32[_S, i] = i;
        }
    }

    private static void process0thElement (int n, int sign, int q0) {
        if (q0 != 0) {
            for (int i = 1; i < n; i++) 
                I32[_Q, i] = I32[_P, i]; // Work on a copy.

            var flips = 1;
            do {
                var qq = I32[_Q, q0];
                if (qq == 0) { // ... until 0th element is 0.
                    sum += sign * flips;
                    if (flips > maxflips) 
                        maxflips = flips; // New maximum?
                    break;
                }

                I32[_Q, q0] = q0;
                if (q0 >= 3) {
                    int i = 1, j = q0 - 1, t;

                    do { 
                        t = I32[_Q, i]; 
                        I32[_Q, i] = I32[_Q, j]; 
                        I32[_Q, j] = t; 
                        i++; 
                        j--; 
                    } while (i < j);
                }

                q0 = qq; flips++;
            } while (true);
        }
    }

    private static void permuteInnerStep (int i) {
        I32[_S, i] = i;

        // Rotate 0<-...<-i+1.
        var t = I32[_P, 0]; 
        for (int j = 0; j <= i; j++) { 
            I32[_P, j] = I32[_P, j + 1]; 
        } 
        I32[_P, i + 1] = t;
    }

    private static bool permuteNegativeSign (int n) {
        int m = n - 1;
        var t = I32[_P, 1]; 
        I32[_P, 1] = I32[_P, 2]; 
        I32[_P, 2] = t; 

        for (int i = 2; i < n; i++) {
            var sx = I32[_S, i];
            if (sx != 0) { 
                I32[_S, i] = sx - 1; 
                break; 
            }
            
            if (i == m)
                return true;

            permuteInnerStep(i);
        }

        return false;
    }

    [Export]
    public static int get_Sum () {
        return sum;
    }

    [Export]
    public static int get_MaxFlips () {
        return maxflips;
    }

    [Export]
    public static void fannkuch (int n) {
        int sign = 1;
        maxflips = 0;
        sum = 0;

        initHeap(n);

        do {
            // Copy and flip.            
            process0thElement(n, sign, I32[_P, 0]);

            // Permute.
            if (sign == 1) {
                var t = I32[_P, 1]; 
                I32[_P, 1] = I32[_P, 0]; 
                I32[_P, 0] = t; 
                sign = -1; // Rotate 0<-1.
            } else {
                sign = 1; // Rotate 0<-1 and 0<-1<-2.
                if (permuteNegativeSign(n))
                    return;
            }
        } while (true);
    }

    public static void Main () {
        SetHeapSize(4096);

        int n = 7;
        Invoke("fannkuch", n);

        AssertEq(228, "get_Sum");
        AssertEq(16,  "get_MaxFlips");
    }
}