#!/bin/bash
set -e

INFILE="third_party/tests/$1.cs"
OUTFILE="output/$1.exe"

# compile if necessary
if [ -f "$OUTFILE" -a "$OUTFILE" -nt "$INFILE" ]; 
then
  echo "// not building $OUTFILE"
  true
else
  echo "// building $OUTFILE"
  mcs /nologo /langversion:6 /debug+ /debug:full $INFILE /reference:WasmMeta/bin/WasmMeta.dll /out:$OUTFILE  
  ./ilwasmc.sh $OUTFILE
  cp WasmMeta/bin/*.dll* output/
fi