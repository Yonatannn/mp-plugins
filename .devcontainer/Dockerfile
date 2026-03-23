FROM mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm

ENV DEBIAN_FRONTEND=noninteractive \
    MISSION_PLANNER_DIR=/opt/mission-planner

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        mono-complete \
        msbuild \
        nuget \
    && mkdir -p /opt/mission-planner \
    && rm -rf /var/lib/apt/lists/*

COPY .devcontainer/mission-planner/ /opt/mission-planner/

WORKDIR /workspaces
