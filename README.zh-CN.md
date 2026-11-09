# xPU StatLine

[English](README.md) | **简体中文**

一行显示 CPU / GPU 实时状态的 Windows 悬浮条：占用率、温度、风扇转速。

```
CPU  12%  55°C  2400 RPM | GPU   3%  45°C     0 RPM
```

目前针对 ASUS ROG Flow X16（CPU + NVIDIA 独显）开发和测试。

## 使用

1. 从 [Releases](https://github.com/Ran-git-hub/XpuStatLine/releases) 下载，运行 `XpuStatLine.exe`，允许管理员权限（读取硬件传感器需要）。
2. 默认快捷键 `Ctrl+Alt+H` 显示 / 隐藏悬浮条。
3. 左键拖动悬浮条；右键悬浮条或使用托盘菜单打开设置（语言、颜色、字号、透明度、快捷键、始终置顶）。
4. 界面支持中文 / English：右键菜单或托盘菜单里点 `English` / `中文` 即可切换，也可在设置里选择。首次启动按系统显示语言自动选择。

设置保存在 `%LOCALAPPDATA%\XpuStatLine\settings.json`。旧版本（HardwareOverlay）的设置会在首次启动时自动迁移。

## 系统要求

- Windows x64
- [.NET 8 Windows Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

无需安装或启动 LibreHardwareMonitor。

## 构建

```
dotnet publish XpuStatLine.csproj -c Release
```

在项目根目录生成单文件 `XpuStatLine.exe`（依赖 .NET 8 Windows Desktop Runtime）。日常调试可用 `dotnet build -c Release`。

`lib` 文件夹包含构建所需的 LibreHardwareMonitorLib 0.9.4 和 HidSharp。

## 已知限制

- “始终置顶”默认开启，但独占全屏游戏（如 CS2）可能遮住普通置顶窗口，可改用游戏的无边框窗口模式。本程序不修改游戏，也不注入游戏进程。
- 传感器名称目前按 Intel/AMD CPU + NVIDIA GPU 匹配；风扇转速通过华硕 ACPI 接口读取，非华硕机型显示 `--`。

## 诊断

```
XpuStatLine.exe --diagnose 输出文件.txt
```

把识别到的传感器读数写入指定文件后退出。

## 许可与致谢

本项目采用 MIT 许可证，见 [LICENSE](LICENSE)。

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)（LibreHardwareMonitorLib，MPL-2.0）
- [HidSharp](https://github.com/IntergatedCircuits/HidSharp)
- 华硕 ACPI 只读接口参考 [G-Helper](https://github.com/seerge/g-helper)
