using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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

        // FIXME: Put this somewhere else
        private static Stream Stdout;

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

        private static IEnumerable<byte> GetHeapRange (int offset, int count) {
            var indices = Enumerable.Range(0, count);
            var bytes = (from i in indices select U8[offset, i]);
            return bytes;
        }

        public static void SetStdout (string filename) {
            if (Stdout != null)
                throw new Exception("Stdout already open");

            Directory.CreateDirectory(Path.Combine("output", "cs-data"));
            // FIXME: Leak
            Stdout = File.OpenWrite(Path.Combine("output", "cs-data", filename));
        }

        public static void Write (int offset, int count) {
            if (Stdout == null)
                throw new Exception("No stdout open");

            var bytes = GetHeapRange(offset, count).ToArray();
            Stdout.Write(bytes, 0, count);
        }
    }

    [JSExternal]
    [JSImmutable]
    public unsafe class HeapI32 {
        public int* Base {
            get {
                return (int*)Heap.Access(0);
            }
        }

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
        public byte* Base {
            get {
                return (byte*)Heap.Access(0);
            }
        }

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
