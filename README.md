# SoftwareLab Mission Planner Plugin

A custom plugin for ArduPilot Mission Planner designed to streamline advanced parameter management and hardware control directly from the HUD.

## Features
* **GPS Control Menu:**
  * Quickly switch between Primary (GPS 1) and Secondary (GPS 2) modules.
  * Toggle GPS Auto Switch mode on the fly.
* **FrSky Parameter Control (JSON Driven):**
  * Load predefined parameter profiles via a simple `PluginConfig.json` file.
  * Easily "Give" or "Take" control with predefined Failsafe/Hardware states.
  * Change parameters without recompiling the plugin.

## Installation
2. Place the downloaded `.dll` file into your Mission Planner `Plugins` folder (usually located at `Documents\Mission Planner\Plugins` or `C:\Program Files (x86)\Mission Planner\Plugins`).
3. Restart Mission Planner.

## Configuration (`PluginConfig.json`)
On the first run, the plugin will automatically generate a `PluginConfig.json` file in the Plugins folder. 
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