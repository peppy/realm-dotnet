#!/bin/bash

SCRIPT_DIRECTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

REALM_CMAKE_CONFIGURATION=Debug
EXTRA_CMAKE_ARGS="-T buildsystem=1"
export REALM_CMAKE_SUBPLATFORM=iOS

for i in "$@"
do
case $i in
  -c=*|--configuration=*)
    REALM_CMAKE_CONFIGURATION="${i#*=}"
    shift
  ;;
  *)
    EXTRA_CMAKE_ARGS="$EXTRA_CMAKE_ARGS $i"
  ;;
esac
done

function build() {
  bash "$SCRIPT_DIRECTORY"/build.sh -c=$REALM_CMAKE_CONFIGURATION -GXcode $EXTRA_CMAKE_ARGS \
    -DCMAKE_SYSTEM_NAME=iOS \
    -DCMAKE_XCODE_ATTRIBUTE_ONLY_ACTIVE_ARCH=NO \
    -DCMAKE_XCODE_ATTRIBUTE_ENABLE_BITCODE=NO \
    -DCMAKE_XCODE_ATTRIBUTE_DYLIB_INSTALL_NAME_BASE="@rpath" \
    -DCMAKE_TRY_COMPILE_TARGET_TYPE=STATIC_LIBRARY \
    -DCMAKE_IOS_INSTALL_COMBINED=YES \
    -DCMAKE_TOOLCHAIN_FILE="$SCRIPT_DIRECTORY/ios.toolchain.cmake"

  # This is a workaround for CMAKE_IOS_INSTALL_COMBINED removing @rpath from LC_DYLIB_ID.
  # Reported here: https://cmake.org/pipermail/cmake/2018-October/068316.html
  rm "$SCRIPT_DIRECTORY"/build/iOS/$REALM_CMAKE_CONFIGURATION/realm-wrappers.framework/realm-wrappers
  xcrun lipo -create "$SCRIPT_DIRECTORY"/cmake/iOS/src/$REALM_CMAKE_CONFIGURATION-iphoneos/realm-wrappers.framework/realm-wrappers \
                     "$SCRIPT_DIRECTORY"/cmake/iOS/src/$REALM_CMAKE_CONFIGURATION-iphonesimulator/realm-wrappers.framework/realm-wrappers \
             -output "$SCRIPT_DIRECTORY"/build/iOS/$REALM_CMAKE_CONFIGURATION/realm-wrappers.framework/realm-wrappers
}

build