#!/bin/sh
# mkreleasebuild Copyright(C) Joshua Hudson 2025

# Apparently reproducible builds actually care about build directory despite debug mode = none

# These are the build parameters; they correspond to the release build

# Version must be changed in the .csproj files as well
VERSION=0.0.6.2
BUILD_PATH=/tmp/build/Emet
SOURCE_DATE_EPOCH=`date -u +%s -d 2025-09-17T02:46:01Z`

export SOURCE_DATE_EPOCH

set -e

netbuildsequence()
{
	# This is one of the more frustrating things I've to deal with in quite awhile.
	# Apparently the process that generates the .xml file is completely broken.
	dotnet build -c Release Emet.FileSystems.csproj

	# But this generates it
	dotnet build -c Release Emet.FileSystems.ref.csproj

	# So here we go and run a custom Nuget packager
	# There's so many bugs in the stock one anyway.
	env -C "$SRCPWD" dotnet run --project ../NugetPacker/NugetPacker.csproj -- "$BUILD_PATH/FileSystems/Emet.FileSystems.csproj"
}

SRCPWD="`pwd`"
mkdir -p $BUILD_PATH
cd ..
tar -cf - patchdosstub.asm emet.png LICENSE GPL3.txt FileSystems/*.csproj FileSystems/*.cs FileSystems/*.md FileSystems/global.json | env -C $BUILD_PATH tar -xf -
cd $BUILD_PATH
nasm -f bin -o patchdosstub patchdosstub.asm
chmod +x patchdosstub
cd FileSystems
netbuildsequence
cd "$SRCPWD"
cp $BUILD_PATH/FileSystems/bin/Release/Emet.FileSystems.$VERSION.nupkg bin/Release/Emet.FileSystems.$VERSION.nupkg
cd $BUILD_PATH
cd FileSystems
mv bin binx
mv obj objx
netbuildsequence
cd "$SRCPWD"
diff $BUILD_PATH/FileSystems/bin/Release/Emet.FileSystems.$VERSION.nupkg bin/Release/Emet.FileSystems.$VERSION.nupkg
rm -rf $BUILD_PATH
