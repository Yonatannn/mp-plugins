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
