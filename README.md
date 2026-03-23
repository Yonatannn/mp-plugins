# SoftwareLab Mission Planner Plugins

This repository now contains three Mission Planner plugins:

* `SoftwareLabGpsPlugin`: the GPS control plugin.
* `SoftwareLabFrSkyPlugin`: the FrSky control plugin.
* `SoftwareLabNotificationsPlugin`: a reusable notifications plugin that renders the popup notifications and can be reused by the other plugins.

All three DLLs are expected to be placed in the Mission Planner `Plugins` folder so the GPS and FrSky plugins can use the shared notifications service.

## Repository Layout

* `SoftwareLabGpsPlugin/`: the standalone GPS control plugin.
* `SoftwareLabFrSkyPlugin/`: the standalone FrSky control plugin.
* `SoftwareLabPlugin.Shared/`: shared source used by both control plugins to avoid duplicated Mission Planner and notification code.
* `SoftwareLabNotificationsPlugin/`: the reusable notifications plugin.
* `build/`: generated build output for each project.

## Plugin Communication

Mission Planner loads plugins into the same process, so the notifications plugin registers a shared `INotificationService`
through `NotificationServiceRegistry`, and both control plugins consume that shared service directly.

## Features

### SoftwareLab GPS Plugin

* **GPS Control Menu**
  * Quickly switch between Primary (GPS 1) and Secondary (GPS 2) modules.
  * Toggle GPS Auto Switch mode on the fly.

### SoftwareLab FrSky Plugin

* **FrSky Parameter Control**
  * Load predefined parameter profiles via `PluginConfig.json`.
  * Easily "Give" or "Take" control with predefined Failsafe or hardware states.
  * Change parameters without recompiling the plugin.

### SoftwareLab Notifications Plugin

* Renders notification popups for SoftwareLab plugins with both auto-close and manual-close modes.
* Exposes a shared `NotificationServiceRegistry` so other plugins can reuse the same notification UI.
* Uses a dark notification theme with larger message text and a close button for manual notifications.

## Build Tasks

The workspace includes four VS Code tasks:

* `Build SoftwareLab Plugins`
* `Build SoftwareLab GPS Plugin`
* `Build SoftwareLab FrSky Plugin`
* `Build SoftwareLab Notifications Plugin`

Each project writes its build output into `build/<ProjectName>/bin/`.

## VS Code Dev Container

The repository includes a Linux-oriented VS Code devcontainer for producing the plugin DLLs in a repeatable way. The image now builds from the lean official `mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim` base image instead of the heavier devcontainers image, which avoids the extra APT repository/key issues that were causing GPG failures during container builds while still providing the .NET SDK needed for `dotnet build`.

### What is included

* .NET SDK in the base devcontainer image for `dotnet build` workflows.
* Mono and NuGet support for the existing Mission Planner plugin projects, while the main build entrypoint remains `dotnet build`.
* A shared solution file, `SoftwareLabPlugins.sln`, so the whole repo can be built from VS Code or the terminal with one command.
* A baked-in `/opt/mission-planner` folder that can be populated from `.devcontainer/mission-planner/` before building the image.

### Mission Planner dependencies

The plugins reference Mission Planner assemblies directly, so these files must exist in `/opt/mission-planner` inside the container:

* `MissionPlanner.exe`
* `MissionPlanner.Comms.dll`
* `MissionPlanner.ArduPilot.dll`
* `MAVLink.dll`
* `Interfaces.dll`
* `Newtonsoft.Json.dll`

The repo resolves those references through the `MissionPlannerDir` MSBuild property. On Linux/devcontainer builds it defaults to `/opt/mission-planner/`, while on Windows it still falls back to the standard Mission Planner install path.

### Recommended offline workflow

1. On a machine with internet access, copy the required Mission Planner files into `.devcontainer/mission-planner/`.
2. Build the image: `bash .devcontainer/build-image.sh mp-plugins-devcontainer:latest`
3. Save the image: `bash .devcontainer/save-image.sh mp-plugins-devcontainer:latest mp-plugins-devcontainer.tar`
4. Transfer the tar file to the offline Linux machine and load it with Docker or Podman.
5. Open this repository in VS Code with the devcontainer and run the build tasks.

### VS Code build flow

The devcontainer is configured so the default VS Code build task runs:

```bash
dotnet build SoftwareLabPlugins.sln --configuration Release
```

Per-plugin tasks also exist for the GPS, FrSky, and Notifications DLLs. If you add a new plugin later, add its project to `SoftwareLabPlugins.sln` and optionally add a dedicated task in `.vscode/tasks.json`.

Run `bash .devcontainer/validate-mission-planner.sh` before building if you want a quick check that the required Mission Planner assemblies are present in the container.

## Installation

1. Build the required plugins.
2. Copy `SoftwareLabGpsPlugin.dll`, `SoftwareLabFrSkyPlugin.dll`, and `SoftwareLabNotificationsPlugin.dll` into the Mission Planner `Plugins` folder.
3. Restart Mission Planner.

## FrSky Configuration (`PluginConfig.json`)

On first run, the FrSky plugin automatically generates `PluginConfig.json` in the Mission Planner `Plugins` folder.
You can edit this file to define which parameters change when clicking the FrSky controls.

```json
{
  "GiveControl": {
    "FS_THR_ENABLE": 0.0
  },
  "TakeControl": {
    "FS_THR_ENABLE": 1.0
  }
}
```

## Reusing Notifications in Other Plugins

Other Mission Planner plugins can reference `SoftwareLabNotificationsPlugin.dll` and use the shared service:

```csharp
using MissionPlanner.SoftwareLab.Notifications;

NotificationServiceRegistry.Current?.ShowMessage(
    "Plugin loaded successfully",
    NotificationSeverity.Success);

NotificationServiceRegistry.Current?.ShowMessage(
    "Mission Planner requires your confirmation",
    NotificationSeverity.Info,
    NotificationDisplayOptions.CreateManualClose());
```
