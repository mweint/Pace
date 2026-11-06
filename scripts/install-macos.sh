#!/bin/sh
# Installs Pace for the current user from an extracted Pace-osx-*.tar.gz.
# Usage: sh install.sh              install or update, then open Pace
#        sh install.sh --uninstall  remove the app (settings and sign-ins are kept)
set -eu

here=$(cd "$(dirname "$0")" && pwd)
apps=$HOME/Applications
target=$apps/Pace.app
agent=$HOME/Library/LaunchAgents/io.github.mweint.pace.plist
support="$HOME/Library/Application Support/Pace"

if [ "${1:-}" = "--uninstall" ]; then
    pkill -x -u "$(id -u)" Pace 2>/dev/null || true
    rm -rf "$target"
    rm -f "$agent"
    echo "Pace removed. Settings remain in $support."
    exit 0
fi

[ -x "$here/Pace.app/Contents/MacOS/Pace" ] || { echo "Run this script from the extracted Pace folder." >&2; exit 1; }

# A running copy holds the single-instance lock, so stop it before replacing the app.
if pkill -x -u "$(id -u)" Pace 2>/dev/null; then
    i=0
    while pgrep -x -u "$(id -u)" Pace >/dev/null 2>&1 && [ $i -lt 50 ]; do sleep 0.1; i=$((i + 1)); done
fi

staging=$apps/.pace-install
rm -rf "$staging"
mkdir -p "$staging"
cp -R "$here/Pace.app" "$staging/"
# This build is not notarized. Clear the download quarantine, then seal the bundle with an ad hoc
# signature for this Mac. Every file beside the executable (including .NET assemblies) is signed
# first, because the bundle signature requires them signed; each binary is already signed.
xattr -cr "$staging/Pace.app"
if ! { find "$staging/Pace.app/Contents/MacOS" -type f -exec codesign --force --sign - {} + &&
       codesign --force --sign - "$staging/Pace.app"; } >/dev/null 2>&1; then
    echo "Warning: the Pace bundle could not be sealed for this Mac; it should still open." >&2
fi
rm -rf "$target"
mv "$staging/Pace.app" "$target"
rmdir "$staging"

open "$target"
echo "Pace installed in $target and opened. Look for its icon in the menu bar."
