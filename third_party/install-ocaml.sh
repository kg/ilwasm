#!/bin/bash

set -e

rm -rf ocaml
mkdir ocaml
pushd ocaml
curl https://wasm.storage.googleapis.com/ocaml-4.02.2.tar.gz -O
CHECKSUM=$(shasum -a 256 ocaml-4.02.2.tar.gz | awk '{ print $1 }')
if [ ${CHECKSUM} != \
  9d50c91ba2d2040281c6e47254c0c2b74d91315dd85cc59b84c5138c3a7ba78c ]; then
  echo "Bad checksum ocaml download checksum!"
  exit 1
fi
tar xfz ocaml-4.02.2.tar.gz
cd ocaml-4.02.2
mkdir ../ocaml-install
./configure -prefix `dirname ../ocaml-install`
make world.opt
make install
popd