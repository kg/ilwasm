#!/bin/bash

SCRIPTPATH=`realpath "$BASH_SOURCE"`
ILWASMDIR=`dirname $SCRIPTPATH`
SEXPRDIR=$ILWASMDIR/../sexpr-wasm-prototype/

MODULEPATH=`realpath "$1"`
BINPATH="$MODULEPATH.v8native"

pushd $SEXPRDIR > /dev/null

out/sexpr-wasm "$MODULEPATH" -o "$BINPATH" --multi-module
BINPATH_ABS=`realpath "$BINPATH"`
third_party/v8-native-prototype/v8/v8/out/Release/d8 test/spec.js "$BINPATH"

popd > /dev/null