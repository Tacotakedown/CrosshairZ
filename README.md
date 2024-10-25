# CrosshairZ

Crosshair overlay for nearly every app/game on Windows.

- Runs in Xbox game bar
- Profiles
- Importing/Exporting crosshairs via codes

![preview](https://raw.githubusercontent.com/Tacotakedown/CrosshairZ/refs/heads/main/Docs/preview.png)

## Requirements

1. Xbox Game Bar
2. VS 2022 with C# UWP workloads installed

## Building

1. Clone the repo
2. Build in release mode
3. App should be in Game Bar widgets

## Installing Via Sideload (releases)
All releases are packages that need to be sideloaded
1. your powershell execution policy needs to be set to unrestricted, do this by running `Set-ExecutionPolicy unrestricted` in an elevated powershell
2. Put your device in devleoper mode, powershell should prompt you to upon installing but if not, go to Windows settings -> System -> For developers, Developer Mode
3. Run `install.ps1` with powershell
