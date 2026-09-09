# Spiritbond Tracker

Spiritbond Tracker is a Dalamud plugin for FINAL FANTASY XIV that tracks equipped-item spiritbond progress, records gains during duties, and displays relevant spiritbond consumables and buffs.

## Features

- Tracks spiritbond percentage for equipped gear
- Shows spiritbond gained during the current duty
- Saves completed duty-session history
- Provides history filtering and statistics
- Shows current location or duty information
- Supports compact and full layouts
- Includes a capped-slot counter and materia-extraction action
- Displays detected spiritbond-related consumables and buffs
- Supports low-overhead Eco Mode, including automatic Field Ops handling

## Installation through Dalamud

The plugin is available after a successful GitHub Actions release.

1. Start FFXIV and enter `/xlsettings`.
2. Open the **Experimental** tab.
3. Find **Custom Plugin Repositories**.
4. Add this URL:

   ```text
   https://raw.githubusercontent.com/Kriss-Hietala/SpiritbondTracker/main/repo.json
   ```

5. Click **Save and Close**.
6. Enter `/xlplugins`.
7. Search for **Spiritbond Tracker**.
8. Click **Install**.
9. Open the plugin with:

   ```text
   /spiritbond
   ```

## Usage

The main window lists equipped gear, current spiritbond percentage, duty-session gain, and an eligibility indication. Use **History** to browse saved session records and **Statistics** to inspect longer-term totals.

The tracker saves progress history in the plugin configuration directory. The **Extract Materia** action is available when items have reached 100% spiritbond.

## Spiritbond consumables

The plugin can display the active Medicated effect, Squadron Spiritbonding Manual, Free Company action, and relevant food status. Medicated effects are source-agnostic, so the plugin deliberately avoids claiming that every Medicated effect came from a Superior Spiritbonding Potion.

## Development build

Requirements:

- .NET 10 SDK
- Dalamud files required by `Dalamud.NET.Sdk/15.0.0`

Build locally:

```bash
~/.dotnet/dotnet build -c Release --no-logo
```

## Releases

Every push to `main` runs GitHub Actions. The workflow automatically assigns version `1.0.<run-number>.0`, builds the Release configuration, creates `publish.zip`, publishes a GitHub Release, and updates the plugin manifests.

The release archive contains:

```text
SpiritbondTracker.dll
SpiritbondTracker.deps.json
SpiritbondTracker.json
```
