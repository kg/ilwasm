#!/bin/bash

mcs /nologo /langversion:6 /debug+ /debug:full third_party/tests/$1.cs /reference:third_party/tests/WasmMeta.dll /out:output/$1.exe
cp -f third_party/tests/WasmMeta.* output
./ilwasmc.sh output/$1.exe