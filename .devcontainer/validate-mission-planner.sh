#!/usr/bin/env bash
set -euo pipefail

mission_planner_dir="${MISSION_PLANNER_DIR:-/opt/mission-planner}"
required_files=(
  "MissionPlanner.exe"
  "MissionPlanner.Comms.dll"
  "MissionPlanner.ArduPilot.dll"
  "MAVLink.dll"
  "Interfaces.dll"
  "Newtonsoft.Json.dll"
)

missing=()
for file in "${required_files[@]}"; do
  if [[ ! -f "$mission_planner_dir/$file" ]]; then
    missing+=("$file")
  fi
done

if (( ${#missing[@]} > 0 )); then
  echo "Mission Planner dependencies are missing from $mission_planner_dir"
  printf 'Missing files:\n'
  printf ' - %s\n' "${missing[@]}"
  echo "Copy the Mission Planner binaries into that directory before building the plugins."
  exit 1
fi

echo "Mission Planner dependencies detected in $mission_planner_dir."
