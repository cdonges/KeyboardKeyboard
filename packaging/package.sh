#!/usr/bin/env bash
# Packages the single-file build in publish/ as a release download, written to dist/.
#   Windows: KeyboardKeyboard.exe
#   Linux:   KeyboardKeyboard-<rid>.tar.gz (a tarball keeps the executable bit)
#   macOS:   KeyboardKeyboard-<rid>.zip containing KeyboardKeyboard.app (must run on macOS, for codesign and ditto)
# Usage, from the repo root: packaging/package.sh <runtime identifier> <version>
set -euo pipefail

rid=$1
version=${2%%-*} # macOS wants a plain x.y.z version, so drop any pre-release suffix

rm -rf dist
mkdir dist

case $rid in
  win-*)
    cp publish/KeyboardKeyboard.exe dist/
    ;;
  linux-*)
    tar -czf "dist/KeyboardKeyboard-$rid.tar.gz" -C publish KeyboardKeyboard
    ;;
  osx-*)
    app=dist/KeyboardKeyboard.app
    mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
    cp publish/KeyboardKeyboard "$app/Contents/MacOS/"
    cp KeyboardKeyboard/app.icns "$app/Contents/Resources/"
    sed "s/@VERSION@/$version/g" packaging/macos/Info.plist > "$app/Contents/Info.plist"
    # Apple Silicon won't run unsigned code, so sign it ad hoc (no developer certificate).
    codesign --force --deep --sign - "$app"
    # ditto keeps the signature and permissions intact, unlike zip.
    ditto -c -k --keepParent "$app" "dist/KeyboardKeyboard-$rid.zip"
    rm -rf "$app"
    ;;
  *)
    echo "Unsupported runtime identifier: $rid" >&2
    exit 1
    ;;
esac

ls -l dist
