#!/bin/bash

SCRIPTPATH=`realpath "$BASH_SOURCE"`
ILWASMDIR=`dirname $SCRIPTPATH`
JSILDIR=$ILWASMDIR/third_party/JSIL

$JSILDIR/bin/JSILc.exe $ILWASMDIR/ilwasm.jsilconfig --quiet --nodefaults --nothreads --e=WasmSExpr "$@"