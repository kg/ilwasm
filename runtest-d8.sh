#!/bin/bash

SCRIPTPATH=`readlink -f "$BASH_SOURCE"`
ILWASMDIR=`dirname $SCRIPTPATH`
SEXPRDIR=$ILWASMDIR/../sexpr-wasm-prototype/

MODULEPATH=`readlink -f "$1"`
BINPATH="$MODULEPATH.v8native"

pushd $SEXPRDIR > /dev/null

out/sexpr-wasm "$MODULEPATH" -o "$BINPATH" --multi-module
BINPATH_ABS=`readlink -f "$BINPATH"`
third_party/v8-native-prototype/v8/v8/out/Release/d8 test/spec.js "$BINPATH"

popd > /dev/null