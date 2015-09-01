#!/bin/bash

TESTNAME=$1
shift
./compile-test.sh $TESTNAME
mono output/$TESTNAME.exe "$@"