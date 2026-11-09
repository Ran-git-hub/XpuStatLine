# xPU StatLine

**English** | [简体中文](README.zh-CN.md)

A one-line Windows overlay that shows live CPU / GPU status: load, temperature and fan speed.

```
CPU  12%  55°C  2400 RPM | GPU   3%  45°C     0 RPM
```

Currently developed and tested on an ASUS ROG Flow X16 (CPU + NVIDIA discrete GPU).

## Usage

1. Download `XpuStatLine.exe` from [Releases](https://github.com/Ran-git-hub/XpuStatLine/releases) and run it. Allow administrator access when prompted (required to read hardware sensors).
2. Press `Ctrl+Alt+H` (default) to show / hide the overlay.
3. Drag the overlay with the left mouse button. Right-click it, or use the tray menu, to open Settings (language, color, text size, opacity, hotkey, always on top).
4. The interface is available in English and Chinese: click `English` / `中文` in the right-click or tray menu to switch, or pick a language in Settings. On first launch it follows the Windows display language.

Settings are stored in `%LOCALAPPDATA%\XpuStatLine\settings.json`. Settings from the previous version (HardwareOverlay) are migrated automatically on first launch.

## Requirements

- Windows x64
- [.NET 8 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

LibreHardwareMonitor does not need to be installed or running.

## Build

```
dotnet publish XpuStatLine.csproj -c Release
```

This produces a single-file `XpuStatLine.exe` in the project root (requires the .NET 8 Windows Desktop Runtime). For day-to-day debugging, use `dotnet build -c Release`.

The `lib` folder contains LibreHardwareMonitorLib 0.9.4 and HidSharp, which are needed to build.

## Known limitations

- "Always on top" is on by default, but exclusive-fullscreen games (such as CS2) can cover regular topmost windows; switch the game to borderless windowed mode instead. This program does not modify games or inject into game processes.
- Sensor names are currently matched for Intel/AMD CPUs + NVIDIA GPUs. Fan speeds are read through the ASUS ACPI interface, so non-ASUS machines show `--`.

## Diagnostics

```
XpuStatLine.exe --diagnose output.txt
```

Writes the detected sensor readings to the given file, then exits.

## License and credits

This project is licensed under the MIT License; see [LICENSE](LICENSE).

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (LibreHardwareMonitorLib, MPL-2.0)
- [HidSharp](https://github.com/IntergatedCircuits/HidSharp)
- The read-only ASUS ACPI interface is based on [G-Helper](https://github.com/seerge/g-helper)
