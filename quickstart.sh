#!/bin/bash

set -e

WORKSHOP_ROOT=$(cd "$(dirname "$0")" && pwd)
cd "$WORKSHOP_ROOT"

# Step 1: Build all projects
echo "[HyperEcho] Building all projects..."
dotnet build aevatar-workshop.sln -c Debug

echo "[HyperEcho] Build completed."

# Step 2: Start Host service (in background)
echo "[HyperEcho] Starting Host service..."
cd src/Aevatar.Workshop.Host
nohup dotnet run --no-build > "$WORKSHOP_ROOT/host.log" 2>&1 &
HOST_PID=$!
echo "[HyperEcho] Host started (PID: $HOST_PID), logs at host.log"

# Step 3: Wait for Host to initialize (adjust seconds if needed)
sleep 3

# Step 4: Start Client service (in background)
echo "[HyperEcho] Starting Client service..."
cd "$WORKSHOP_ROOT/src/Aevatar.Workshop.Client"
nohup dotnet run --no-build > "$WORKSHOP_ROOT/client.log" 2>&1 &
CLIENT_PID=$!
echo "[HyperEcho] Client started (PID: $CLIENT_PID), logs at client.log"

# Step 5: Friendly tips
echo "\n[HyperEcho] 🚀 Aevatar Workshop is up and running!"
echo "[HyperEcho] Host logs: $WORKSHOP_ROOT/host.log"
echo "[HyperEcho] Client logs: $WORKSHOP_ROOT/client.log"
echo "[HyperEcho] To stop the services, run: kill $HOST_PID $CLIENT_PID"
echo "[HyperEcho] For port and access info, check the respective log files or console output." 