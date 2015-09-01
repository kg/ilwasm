# ilwasm
Maps a subset of [.NET CIL](https://en.wikipedia.org/wiki/Common_Intermediate_Language) to WebAssembly S-expressions. Implemented as a custom code generator backend for the [JSIL](https://github.com/sq/JSIL) CIL -> JS compiler.

## Building on Windows
Make sure you have Visual Studio 2015 (the free Community edition is fine). Build JSIL (```third_party/JSIL/JSIL.sln```), then build WasmSExprEmitter & WasmMeta (```WasmSExprEmitter.sln```).

## Building on Linux
A recent version of Mono is required for C#6 support and NuGet compatibility. If you don't have a recent version of Mono installed, run ```third_party/JSIL/install-mono.sh``` once to download & build from source, then do ```source third_party/JSIL/activate-mono.sh``` to load it into your current environment.

Build JSIL (```third_party/JSIL/linux-build.sh```), then build WasmSExprEmitter & WasmMeta (```linux-build.sh```).

## Writing tests
Tests are placed in the ```third_party/tests``` directory as .cs files. A test has a single entry point (typically ```Program.Main```) that accepts no arguments and has no return value.

The entry point's body must contain only calls to APIs from ```Wasm.Test```, like ```SetHeapSize``` or ```AssertEq```. Constants are fine but locals, branching or other behavior are not allowed in the entry point, as it is converted into top-level wasm assertions.

### Exports

A test can export static methods with the Wasm.Module.Export attribute, like so:
```csharp
[Export]
public static void Exported () {}
[Export("customName")]
public static void AlsoExported () {}
```
Exported methods can be invoked by name in the entry point by using the ```Test.AssertEq``` and ```Test.Invoke``` APIs.

### Heap

Tests requiring access to the heap must first reserve it using the ```Test.SetHeapSize``` API. To read/write the heap use the fields of the ```Wasm.Heap``` class, like so:
```csharp
Wasm.Heap.I32[offset] = value;
value = Wasm.Heap.U8[@base, offset];
```

The heap is not accessible externally (i.e. in the entry point) so to perform assertions against heap data, you must expose the heap via an exported function.

### Fields and properties

Static fields of supported types work. To access a field's value from outside, export accessor methods, or simply define it as an exported property:
```csharp
[Export]
public static int Property { get; set; }
```

### Types

Currently only integral and floating-point types are supported. Functions and variables of other types will be ignored (or outright rejected) by ilwasm.

### Future features

* Structs
* ref/out parameters
* Pointers
