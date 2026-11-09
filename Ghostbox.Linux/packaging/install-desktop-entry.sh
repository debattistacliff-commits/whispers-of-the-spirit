#!/bin/sh
# Adds Whispers of the Spirit to your applications menu (for your user only; no admin rights needed).
# Run it again if you move this folder. Remove with: rm ~/.local/share/applications/whispers-of-the-spirit.desktop
here=$(dirname "$(readlink -f "$0")")
apps="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
mkdir -p "$apps"
cat > "$apps/whispers-of-the-spirit.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Whispers of the Spirit
Comment=Radio investigation console: sweep, listen, record and review
Exec="$here/whispers-of-the-spirit"
Icon=$here/app/whispers-of-the-spirit.png
Terminal=false
Categories=AudioVideo;Audio;
EOF
chmod +x "$apps/whispers-of-the-spirit.desktop"
echo "Added to the applications menu: $apps/whispers-of-the-spirit.desktop"
