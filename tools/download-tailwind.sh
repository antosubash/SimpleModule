#!/usr/bin/env bash
set -euo pipefail
VERSION="v4.1.3"
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
case "$OS" in darwin) OS="macos" ;; esac
ARCH=$(uname -m)
case "$ARCH" in x86_64) ARCH="x64" ;; aarch64|arm64) ARCH="arm64" ;; esac
FILENAME="tailwindcss-${OS}-${ARCH}"
URL="https://github.com/tailwindlabs/tailwindcss/releases/download/${VERSION}/${FILENAME}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${SCRIPT_DIR}/tailwindcss"

if [ -f "$OUT" ]; then echo "Tailwind CLI already exists"; exit 0; fi

echo "Downloading Tailwind CSS ${VERSION}..."
curl -sL "$URL" -o "$OUT"
chmod +x "$OUT"
echo "Downloaded to $OUT"
