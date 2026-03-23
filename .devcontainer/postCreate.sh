#!/usr/bin/env bash
set -euo pipefail

echo "Devcontainer startup hook running."
echo "MISSION_PLANNER_DIR=${MISSION_PLANNER_DIR:-unset}"
dotnet --info | sed -n '1,12p'
mono --version | head -n 1
