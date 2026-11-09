# xPU StatLine

**English** | [简体中文](README.zh-CN.md)

A one-line Windows overlay that shows live CPU / GPU status: load, temperature and fan speed.

```
CPU  12%  55°C  2400 RPM | GPU   3%  45°C     0 RPM
```

Currently developed and tested on an ASUS ROG Flow X16 (CPU + NVIDIA discrete GPU).

## Usage

1. Download `XpuStatLine.exe` from [Releases](https://github.com/Ran-git-hub/XpuStatLine/releases) and run it. No installation or administrator rights needed.
2. Press `Ctrl+Alt+H` (default) to show / hide the overlay.
3. Drag the overlay with the left mouse button. Right-click it, or use the tray menu, to open Settings (language, color, text size, opacity, hotkey, always on top).
4. The interface is available in English and Chinese: click `English` / `中文` in the right-click or tray menu to switch, or pick a language in Settings. On first launch it follows the Windows display language.

Settings are stored in `%LOCALAPPDATA%\XpuStatLine\settings.json`. Settings from the previous version (HardwareOverlay) are migrated automatically on first launch.

## Requirements

- Windows x64
- [.NET 8 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- NVIDIA graphics driver (for GPU load / temperature)

No kernel driver is installed: CPU load comes from Windows (`GetSystemTimes`), GPU load and temperature from NVIDIA NVML (`nvml.dll`, part of the NVIDIA driver), and CPU temperature and fan speeds from the read-only ASUS ACPI interface.

## Build

```
dotnet publish XpuStatLine.csproj -c Release
```

This produces a single-file `XpuStatLine.exe` in the project root (requires the .NET 8 Windows Desktop Runtime). For day-to-day debugging, use `dotnet build -c Release`.

## Known limitations

- "Always on top" is on by default, but exclusive-fullscreen games (such as CS2) can cover regular topmost windows; switch the game to borderless windowed mode instead. This program does not modify games or inject into game processes.
- GPU readings support NVIDIA GPUs only. CPU temperature and fan speeds are read through the ASUS ACPI interface, so non-ASUS machines show `--` for them.
- The exe is not code-signed. When downloaded from the internet, Windows SmartScreen may warn on first run; choose "More info" → "Run anyway".

## Diagnostics

```
XpuStatLine.exe --diagnose output.txt
```

Writes the detected sensor readings to the given file, then exits.

## License and credits

This project is licensed under the MIT License; see [LICENSE](LICENSE).

- The read-only ASUS ACPI interface is based on [G-Helper](https://github.com/seerge/g-helper)
