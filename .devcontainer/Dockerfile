FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim

ARG USERNAME=vscode
ARG USER_UID=1000
ARG USER_GID=${USER_UID}

ENV DEBIAN_FRONTEND=noninteractive \
    MISSION_PLANNER_DIR=/opt/mission-planner

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        mono-complete \
        nuget \
        sudo \
    && groupadd --gid "${USER_GID}" "${USERNAME}" \
    && useradd --uid "${USER_UID}" --gid "${USER_GID}" -m "${USERNAME}" \
    && echo "${USERNAME} ALL=(root) NOPASSWD:ALL" > "/etc/sudoers.d/${USERNAME}" \
    && chmod 0440 "/etc/sudoers.d/${USERNAME}" \
    && mkdir -p /opt/mission-planner /workspaces \
    && chown -R "${USERNAME}:${USERNAME}" /opt/mission-planner /workspaces \
    && rm -rf /var/lib/apt/lists/*

COPY .devcontainer/mission-planner/ /opt/mission-planner/

USER ${USERNAME}
WORKDIR /workspaces
