# SoftwareLab Mission Planner Plugins

This repository now contains two Mission Planner plugins:

* `SoftwareLabPlugin`: the controls plugin that contains the GPS and FrSky features.
* `SoftwareLabNotificationsPlugin`: a reusable notifications plugin that renders the popup notifications and can be reused by other plugins.

Both plugins are expected to be placed in the Mission Planner `Plugins` folder so the controls plugin can use the shared notifications service.

## Repository Layout

* `SoftwareLabPlugin/`: the main controls plugin.
* `SoftwareLabNotificationsPlugin/`: the reusable notifications plugin.
* `build/`: generated build output for each project.

## Plugin Communication

Mission Planner loads plugins into the same process, so the notifications plugin registers a shared `INotificationService`
through `NotificationServiceRegistry`, and the controls plugin consumes that shared service directly.

## Features

### SoftwareLab Controls Plugin

* **GPS Control Menu**
  * Quickly switch between Primary (GPS 1) and Secondary (GPS 2) modules.
  * Toggle GPS Auto Switch mode on the fly.
* **FrSky Parameter Control**
  * Load predefined parameter profiles via `PluginConfig.json`.
  * Easily "Give" or "Take" control with predefined Failsafe or hardware states.
  * Change parameters without recompiling the plugin.

### SoftwareLab Notifications Plugin

* Renders auto-closing notification popups for SoftwareLab plugins.
* Exposes a shared `NotificationServiceRegistry` so other plugins can reuse the same notification UI.
* Uses a Mission Planner styled background and a smaller popup window size than the original in-plugin implementation.

## Build Tasks

The workspace includes two VS Code tasks:

* `Build SoftwareLab Controls Plugin`
* `Build SoftwareLab Notifications Plugin`

Each project writes its build output into `build/<ProjectName>/bin/`.

## Installation

1. Build both plugins.
2. Copy `SoftwareLabPlugin.dll` and `SoftwareLabNotificationsPlugin.dll` into the Mission Planner `Plugins` folder.
3. Restart Mission Planner.

## Configuration (`PluginConfig.json`)

On first run, the controls plugin automatically generates `PluginConfig.json` in the Mission Planner `Plugins` folder.
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
```
