#!/bin/sh

# Apparently reproducible builds actually care about build directory despite debuug mode = none

# These are the build parameters; they correspond to the release build

# Version must be changed in the .csproj files as well
VERSION=0.0.6.1
BUILD_PATH=/tmp/build/Emet
SOURCE_DATE_EPOCH=`date -u +%s -d 2025-04-29T3:00:01Z`

export SOURCE_DATE_EPOCH

# One tool isn't ready for external deploy yet

if [ ! -f "$1"/Kuinox.NupkgDeterministicator.csproj ]
then	echo "USAGE: $0 /path/to/Kuionx.NupkgDeterministicator"
	exit 1
fi

if grep -q SOURCE_DATE_EPOCH "$1"/Program.cs
then	:
else	echo "Error: Kuionx.NupkgDterministicator does not support SOURCE_DATE_EPOCH"
	exit 1
fi

set -e

SRCPWD="`pwd`"
mkdir -p $BUILD_PATH
cd ..
tar -cf - patchdosstub.asm emet.png LICENSE GPL3.txt FileSystems/*.csproj FileSystems/*.cs FileSystems/*.md FileSystems/global.json | env -C $BUILD_PATH tar -xf -
cd $BUILD_PATH
nasm -f bin -o patchdosstub patchdosstub.asm
chmod +x patchdosstub
cd FileSystems
dotnet pack -c Release Emet.FileSystems.csproj
cd "$SRCPWD"
env -C "$1" dotnet run $BUILD_PATH/FileSystems/bin/Release/Emet.FileSystems.$VERSION.nupkg
cp $BUILD_PATH/FileSystems/bin/Release/Emet.FileSystems.$VERSION.nupkg bin/Release/Emet.FileSystems.$VERSION.nupkg
cd $BUILD_PATH
cd FileSystems
mv bin binx
mv obj objx
dotnet pack -c Release Emet.FileSystems.csproj
env -C "$1" dotnet run $BUILD_PATH/FileSystems/bin/Release/Emet.FileSystems.$VERSION.nupkg
cd "$SRCPWD"
diff $BUILD_PATH/FileSystems/bin/Release/Emet.FileSystems.$VERSION.nupkg bin/Release/Emet.FileSystems.$VERSION.nupkg
rm -rf $BUILD_PATH
