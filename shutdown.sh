#!/bin/bash

# Quick shut down script for Aevatar Workshop
# Finds and kills running Host and Client processes

echo "[Aevatar Workshop] Searching for running Host and Client processes..."

HOST_PIDS=$(ps aux | grep 'Aevatar.Workshop.Host' | grep -v grep | awk '{print $2}')
CLIENT_PIDS=$(ps aux | grep 'Aevatar.Workshop.Client' | grep -v grep | awk '{print $2}')

if [ -z "$HOST_PIDS" ] && [ -z "$CLIENT_PIDS" ]; then
  echo "[Aevatar Workshop] No Host or Client processes found."
  exit 0
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

echo "[Aevatar Workshop] Done." 