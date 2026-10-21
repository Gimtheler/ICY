# ICY

Windows display utility with a simple overlay. It is made for resolution changes, screen color tweaks and a static crosshair window.


## What it does

* Change resolution and refresh rate
* Adjust brightness, contrast, gamma and saturation
* Save lighting presets
* Show a simple overlay crosshair
* Run from the system tray
* Ask before keeping a display change

## What it does not do

* It does not read another process memory
* It does not inject DLLs
* It does not install drivers
* It does not automate mouse or keyboard input
* It does not hook DirectX, OpenGL or Vulkan
* It does not detect games by window title or exe name
* It does not capture your screen for aim help

The overlay is a normal top level window. Some exclusive fullscreen apps hide windows like this, so the overlay may not show there.

## Build it

You need Windows 10 or 11 and the .NET 8 SDK.

Open a terminal in `Source Code` and run:

```bat
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

The app is written to:

```text
Source Code\bin\Release\net8.0-windows\ICY.exe
```

## Notes

Settings are stored in `%AppData%\ICY\`. The app does not search game folders or other app folders.

Use it at your own risk and follow the rules of any software or service you use it with.

## License

MIT. See `Source Code/LICENSE`.

Original project links are kept in the source:

* https://github.com/soh4p
* https://github.com/soh4p/ICY
