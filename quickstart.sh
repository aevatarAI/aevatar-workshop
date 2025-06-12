#!/bin/bash

set -e

WORKSHOP_ROOT=$(cd "$(dirname "$0")" && pwd)
cd "$WORKSHOP_ROOT"

# Parse arguments for Client
MODE=${1:-0}
GREETING=${2:-}

# Step 1: Build all projects
echo "[Aevatar Workshop] Building all projects..."
dotnet build aevatar-workshop.sln -c Debug

echo "[Aevatar Workshop] Build completed."

# Step 2: Start Host service (in background)
echo "[Aevatar Workshop] Starting Host service..."
cd src/Aevatar.Workshop.Host
nohup dotnet run --no-build > "$WORKSHOP_ROOT/host.log" 2>&1 &
HOST_PID=$!
echo "[Aevatar Workshop] Host started (PID: $HOST_PID), logs at host.log"

# Step 3: Wait for Host to initialize (adjust seconds if needed)
sleep 3

# Step 4: Start Client service (in background)
echo "[Aevatar Workshop] Starting Client service..."
cd "$WORKSHOP_ROOT/src/Aevatar.Workshop.Client"
if [ -z "$GREETING" ]; then
  nohup dotnet run --no-build -- "$MODE" > "$WORKSHOP_ROOT/client.log" 2>&1 &
else
  nohup dotnet run --no-build -- "$MODE" "$GREETING" > "$WORKSHOP_ROOT/client.log" 2>&1 &
fi
CLIENT_PID=$!
echo "[Aevatar Workshop] Client started (PID: $CLIENT_PID), logs at client.log"

# Step 5: Friendly tips
echo "\n[Aevatar Workshop] 🚀 Aevatar Workshop Projects is up and running!"
echo "[Aevatar Workshop] Host logs: $WORKSHOP_ROOT/host.log"
echo "[Aevatar Workshop] Client logs: $WORKSHOP_ROOT/client.log"
echo "[Aevatar Workshop] To stop the services, run: kill $HOST_PID $CLIENT_PID"
echo "[Aevatar Workshop] For port and access info, check the respective log files or console output." 