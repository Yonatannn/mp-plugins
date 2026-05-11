#!/usr/bin/env bash
set -euo pipefail

engine="${CONTAINER_ENGINE:-}"
if [[ -z "$engine" ]]; then
  if command -v docker >/dev/null 2>&1; then
    engine=docker
  elif command -v podman >/dev/null 2>&1; then
    engine=podman
  else
    echo "Neither docker nor podman is installed."
    exit 1
  fi
fi

image_tag="${1:-mp-plugins-devcontainer:latest}"
archive_path="${2:-mp-plugins-devcontainer.tar}"
"$engine" save "$image_tag" -o "$archive_path"
echo "Saved $image_tag to $archive_path"
