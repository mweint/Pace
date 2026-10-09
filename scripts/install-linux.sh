#!/bin/sh
# Installs Pace for the current user from an extracted Pace-linux-x64.tar.gz.
# Usage: ./install.sh            install or update
#        ./install.sh --uninstall remove the app (settings and sign-ins are kept)
set -eu

here=$(cd "$(dirname "$0")" && pwd)
data=${XDG_DATA_HOME:-$HOME/.local/share}
config=${XDG_CONFIG_HOME:-$HOME/.config}
target=$data/pace
launcher=$HOME/.local/bin/pace
entry=$data/applications/pace.desktop
icon=$data/icons/hicolor/256x256/apps/pace.png

refresh_menus() {
    command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$data/applications" >/dev/null 2>&1 || true
    command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -q -t "$data/icons/hicolor" >/dev/null 2>&1 || true
}

if [ "${1:-}" = "--uninstall" ]; then
    pkill -x -u "$(id -u)" Pace 2>/dev/null || true
    rm -rf "$target"
    rm -f "$entry" "$icon" "$config/autostart/pace.desktop"
    [ -L "$launcher" ] && rm -f "$launcher"
    refresh_menus
    echo "Pace removed. Settings remain in $config/Pace."
    exit 0
fi

[ -x "$here/app/Pace" ] || { echo "Run this script from the extracted Pace folder." >&2; exit 1; }

# A running copy holds the single-instance lock, so stop it now and restart it after installing.
running=false
if pkill -x -u "$(id -u)" Pace 2>/dev/null; then
    running=true
    i=0
    while pgrep -x -u "$(id -u)" Pace >/dev/null 2>&1 && [ $i -lt 50 ]; do sleep 0.1; i=$((i + 1)); done
fi

# Replace the folder rather than overwriting files, so a running copy keeps working until it restarts.
staging=$target.new
rm -rf "$staging"
mkdir -p "$data"
cp -R "$here/app" "$staging"
chmod +x "$staging/Pace"
rm -rf "$target"
mv "$staging" "$target"

mkdir -p "$(dirname "$launcher")" "$(dirname "$entry")" "$(dirname "$icon")"
ln -sfn "$target/Pace" "$launcher"
cp "$here/pace.png" "$icon"

# Desktop Entry Exec quoting: escape \ " ` $ inside quotes, double backslashes, and write % as %%.
exec_path=$(printf '%s' "$target/Pace" | sed -e 's/\\/\\\\\\\\/g' -e 's/["`$]/\\\\&/g' -e 's/%/%%/g')
cat > "$entry" <<EOF
[Desktop Entry]
Type=Application
Name=Pace
Comment=Claude and Codex usage in the system tray
Exec="$exec_path"
Icon=pace
Terminal=false
Categories=Utility;
StartupWMClass=Pace
EOF
refresh_menus

if $running; then
    nohup "$target/Pace" >/dev/null 2>&1 &
    echo "Pace updated and restarted."
else
    echo "Pace installed. Open it from your applications menu, or run: $target/Pace"
fi
case ":$PATH:" in *":$HOME/.local/bin:"*) ;; *) echo "Add ~/.local/bin to PATH to run 'pace' from a terminal." ;; esac
