# ICY

**V1.0** | MIT License | [Check for updates](https://github.com/soh4p/ICY)

Run **Setup.exe** to install **Application.exe**. Source is in **OpenSource**.

ICY is a Windows desktop utility for **OS-level display tweaks** and an optional **plain overlay window**.

It can:

- Change a monitor’s resolution and refresh rate
- Adjust global display color (brightness, contrast, gamma, saturation)
- Save generic lighting presets (for example “Bright room”)
- Draw a static crosshair as a separate top-level window

ICY is a normal user-mode WPF app. It does not interact with any other process.

## Safety

ICY is designed so it **cannot** operate as a game cheat or injector.

**ICY does not:**

- Access game processes or files
- Read or write another process’s memory
- Inject DLLs, install hooks, or load kernel drivers
- Automate mouse, keyboard, or controller input
- Hook DirectX, OpenGL, or Vulkan
- Detect specific games or change behavior from window titles / executable names
- Read the screen, run computer vision, or expose ESP / aim assistance

**ICY only:**

- Calls documented Windows display APIs (`EnumDisplayDevices`, `EnumDisplaySettingsEx`, `ChangeDisplaySettingsEx`, `GetDeviceGammaRamp`, `SetDeviceGammaRamp`)
- Optionally applies a desktop-wide saturation matrix through the Magnification **color-effect** API (`MagSetFullscreenColorEffect`), never zoom or pixel capture
- Draws its own layered, click-through window so DWM can composite a static graphic

Presets are lighting snapshots only. There is no “profile per game” and no automation that turns settings on when a program starts.

Activity logging records ICY operations only (mode changes, color applies, overlay on/off). It does not record other processes or window titles.

Settings live in `%AppData%\ICY\` (`settings.json`, `presets.json`, `activity.log`). ICY never searches Steam, Epic, Riot, or other application folders.

## Requirements

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build
- .NET 8 Desktop Runtime to run (included with the SDK)

## Build and run

```bat
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

The executable is written to `bin\Release\net8.0-windows\ICY.exe`.

## Using ICY

1. **Display**: pick a monitor and a resolution/refresh mode, then apply. ICY asks you to keep or revert the desktop timing (15 seconds).
2. **Color**: move the sliders. Brightness / contrast / gamma use the monitor gamma ramp. Saturation uses a desktop compositor matrix when Windows allows it. Click **Reset to default** to restore a neutral LUT. Color changes are restored when ICY exits.
3. **Overlay**: enable a click-through crosshair window. Position it with the offset sliders. Exclusive-fullscreen apps may hide all DWM windows; ICY will not hook a renderer to force the overlay on top.
4. **Presets**: apply built-in lighting presets or save your own names. Names are yours; ICY does not bind them to programs.
5. **Settings**: run as administrator, and optional activity log / debug console (both off by default).

## Architecture

| Area | Implementation | Why it stays OS-only |
| --- | --- | --- |
| Resolution / refresh | `ChangeDisplaySettingsEx` on `\\.\DISPLAYn` | Same path as Windows Settings; monitor-scoped |
| Brightness / contrast / gamma | `SetDeviceGammaRamp` on a display DC | Hardware/OS LUT, not an app hook |
| Saturation | `MagSetFullscreenColorEffect` | Desktop color filter; no magnification / no capture |
| Overlay | Separate WPF window + `WS_EX_LAYERED` / `TRANSPARENT` / `TOPMOST` | DWM compositing only |
| Presets | `%AppData%\ICY\presets.json` | No game paths, no process watchers |

Color adjustments apply while ICY is running and are restored on exit. Desktop resolution changes are written to the user’s display settings (with a revert prompt).
