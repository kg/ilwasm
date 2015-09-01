#!/bin/bash
set -e

TESTNAME=$1
shift
./compile-test.sh $TESTNAME
mono output/$TESTNAME.exe "$@"