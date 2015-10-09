#!/bin/bash

SCRIPTPATH=`realpath "$BASH_SOURCE"`
ILWASMDIR=`dirname $SCRIPTPATH`
JSILDIR=$ILWASMDIR/third_party/JSIL

pushd $ILWASMDIR

rm -rf output

echo // Building ilwasm
xbuild WasmSExprEmitter.sln /v:m

popd