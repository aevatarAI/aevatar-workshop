#!/bin/bash

set -e

DOCS_DIR="$(cd "$(dirname "$0")/docs" && pwd)"
PORT=3000

cd "$DOCS_DIR"

# Check for pnpm
if ! command -v pnpm >/dev/null 2>&1; then
  echo "[Aevatar Workshop] pnpm is not installed."
  read -p "Do you want to install pnpm globally now? [Y/n] " yn
  case $yn in
    [Nn]*) echo "[Aevatar Workshop] Please install pnpm manually and rerun this script."; exit 1;;
    *)
      npm install -g pnpm
      ;;
  esac
fi

echo "[Aevatar Workshop] Installing docs dependencies with pnpm..."
pnpm install

echo "[Aevatar Workshop] Starting docs site..."
pnpm start &
SERVER_PID=$!

sleep 3

# Try to open browser
if command -v open >/dev/null 2>&1; then
  open "http://localhost:$PORT"
elif command -v xdg-open >/dev/null 2>&1; then
  xdg-open "http://localhost:$PORT"
else
  echo "[Aevatar Workshop] Please open http://localhost:$PORT in your browser."
fi

wait $SERVER_PID 