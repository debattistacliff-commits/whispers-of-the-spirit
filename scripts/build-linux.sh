#!/usr/bin/env bash
# Builds the Linux x64 app and packs it as out/Whispers-of-the-Spirit-<version>-Linux-x64.tar.gz.
# Run from anywhere on Linux (or WSL) with the .NET 10 SDK installed:  ./scripts/build-linux.sh
set -euo pipefail

version="2.0"
name="Whispers-of-the-Spirit-$version-Linux-x64"
root="$(cd "$(dirname "$0")/.." && pwd)"
out="${1:-$root/out}"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

stage="$work/$name"
# --artifacts-path keeps Linux build files out of the projects' own obj/bin folders,
# so Windows/Android builds of the same checkout are not disturbed.
dotnet publish "$root/Ghostbox.Linux/Ghostbox.Linux.csproj" -c Release -r linux-x64 --self-contained true \
  --artifacts-path "$work/artifacts" -o "$stage/app" -p:DebugType=none -p:GenerateDocumentationFile=false

install -m 755 "$root/Ghostbox.Linux/packaging/whispers-of-the-spirit" "$stage/whispers-of-the-spirit"
install -m 755 "$root/Ghostbox.Linux/packaging/install-desktop-entry.sh" "$stage/install-desktop-entry.sh"
install -m 644 "$root/Ghostbox.Linux/packaging/README-LINUX.txt" "$stage/README-LINUX.txt"
chmod 755 "$stage/app/WhispersOfTheSpirit"

mkdir -p "$out"
tar -C "$work" --owner=0 --group=0 -czf "$out/$name.tar.gz" "$name"
echo "Linux package: $out/$name.tar.gz"
