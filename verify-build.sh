#!/bin/bash

pushd "${0%/*}" > /dev/null 
cd test/docker
dotnet clean
dotnet build
dotnet test
popd
