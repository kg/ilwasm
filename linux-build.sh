#!/bin/bash

SCRIPTPATH=`realpath "$BASH_SOURCE"`
ILWASMDIR=`dirname $SCRIPTPATH`
JSILDIR=$ILWASMDIR/third_party/JSIL

pushd $ILWASMDIR

echo // Building ilwasm
xbuild WasmSExprEmitter.sln /v:m

popd