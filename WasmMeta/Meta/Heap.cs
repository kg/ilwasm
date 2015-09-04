using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using JSIL.Meta;

namespace Wasm {
    [JSExternal]
    public unsafe static class Heap {
        internal struct State {
            internal readonly GCHandle BufferPin;
            internal readonly byte[]   Buffer;
            internal readonly byte*    pBuffer;

            public State (int size) {
                Buffer = new byte[size];
                BufferPin = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
                pBuffer = (byte *)BufferPin.AddrOfPinnedObject();
            }

            public void Dispose () {
                if (BufferPin.IsAllocated)
                    BufferPin.Free();
            }
        }

        private static State Allocated;
        public static readonly HeapU8  U8  = new HeapU8();
        public static readonly HeapI32 I32 = new HeapI32();

        public static void SetHeapSize (int size) {
            Allocated.Dispose();
            Allocated = new State(size);
        }

        internal static byte* Access (int desiredOffset) {
            if (Allocated.Buffer == null)
                throw new InvalidOperationException("Heap not allocated");
            else if (desiredOffset >= Allocated.Buffer.Length)
                throw new Exception(string.Format("Attempted to access offset {0} but heap size is {1}", desiredOffset, Allocated.Buffer.Length));

            return Allocated.pBuffer;
        }
    }

    [JSExternal]
    [JSImmutable]
    public unsafe class HeapI32 {
        public int this [int offset] {
            get {
                int* ints = (int*)Heap.Access(offset * 4);
                return ints[offset];
            }

            set {
                int* ints = (int*)Heap.Access(offset * 4);
                ints[offset] = value;
            }
        }

        public int this [int @base, int offset] {
            get {
                return this[@base + offset];
            }
            set {
                this[@base + offset] = value;
            }
        }
    }

    [JSExternal]
    [JSImmutable]
    public unsafe class HeapU8 {
        public byte this [int offset] {
            get {
                var bytes = Heap.Access(offset);
                return bytes[offset];
            }

            set {
                var bytes = Heap.Access(offset);
                bytes[offset] = value;
            }
        }

        public byte this [int @base, int offset] {
            get {
                return this[@base + offset];
            }
            set {
                this[@base + offset] = value;
            }
        }
    }
}
