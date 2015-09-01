#!/bin/bash

rm last-test-run.log

for filename in third_party/tests/*.cs; do
  SHORTNAME=${filename##*/}
  ./compile-and-run-test.sh ${SHORTNAME%%.*} --quiet >> last-test-run.log 2>> last-test-run.log

  if [ $? -ne 0 ]; then
    echo -ne F
  else
    echo -ne .
  fi
done

echo
echo Done.

cat last-test-run.log