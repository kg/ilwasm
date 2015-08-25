@csc /nologo /langversion:6 /debug+ /debug:full third_party\tests\%1.cs /reference:third_party\tests\WasmMeta.dll /out:output\%1.exe
@copy third_party\tests\WasmMeta.dll output\WasmMeta.dll > NUL
@call ilwasmc output\%1.exe