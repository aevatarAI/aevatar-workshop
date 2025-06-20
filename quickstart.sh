#!/bin/bash

set -e

WORKSHOP_ROOT=$(cd "$(dirname "$0")" && pwd)
cd "$WORKSHOP_ROOT"

# Always shut down any running host/client before starting
echo "[Aevatar Workshop] Searching for running Host and Client processes..."

HOST_PIDS=$(ps aux | grep 'Aevatar.Workshop.Host' | grep -v grep | awk '{print $2}')
CLIENT_PIDS=$(ps aux | grep 'Aevatar.Workshop.Client' | grep -v grep | awk '{print $2}')

if [ -z "$HOST_PIDS" ] && [ -z "$CLIENT_PIDS" ]; then
  echo "[Aevatar Workshop] No Host or Client processes found."
fi

if [ -n "$HOST_PIDS" ]; then
  echo "[Aevatar Workshop] Host PIDs: $HOST_PIDS"
  kill -9 $HOST_PIDS
  echo "[Aevatar Workshop] Killed Host process(es)."
fi

if [ -n "$CLIENT_PIDS" ]; then
  echo "[Aevatar Workshop] Client PIDs: $CLIENT_PIDS"
  kill -9 $CLIENT_PIDS
  echo "[Aevatar Workshop] Killed Client process(es)."
fi

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
printf "[Aevatar Workshop] Waiting for Host to initialize (10s countdown)... "
for i in {10..1}; do
  printf "%s " "$i"
  sleep 1
done
printf "\n"

# Set HOST_LOG_PATH environment variable for client
export HOST_LOG_PATH="$WORKSHOP_ROOT/host.log"
export CLIENT_LOG_PATH="$WORKSHOP_ROOT/client.log"

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
echo "[Aevatar Workshop] 🚀 Aevatar Workshop Projects is up and running!"
echo "[Aevatar Workshop] Host logs: $WORKSHOP_ROOT/host.log"
echo "[Aevatar Workshop] Client logs: $WORKSHOP_ROOT/client.log"
echo "[Aevatar Workshop] To stop the services, run: kill $HOST_PID $CLIENT_PID"
echo "[Aevatar Workshop] Or run: sh shutdown.sh"
echo "[Aevatar Workshop] For port and access info, check the respective log files or console output."
echo "[Aevatar Workshop] Please open http://localhost:5000 in your browser."