using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ICY.Models;

namespace ICY.Services;

/// <summary>
/// Persists ICY UI state under %AppData%\ICY\settings.json.
/// Does not store process names, window titles, or paths to other applications.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(IcyPaths.SettingsFile))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(IcyPaths.SettingsFile);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
            if (dto is null)
            {
                return new AppSettings();
            }

            var settings = new AppSettings
            {
                SelectedMonitorDeviceName = dto.SelectedMonitorDeviceName,
                ShowAllDisplayModes = dto.ShowAllDisplayModes,
                LoggingEnabled = dto.LoggingEnabled,
                ConsoleEnabled = dto.ConsoleEnabled,
                AutomaticStartup = dto.AutomaticStartup,
                MinimizeToTrayOnClose = dto.MinimizeToTrayOnClose,
                StartMinimized = dto.StartMinimized
            };

            if (dto.Color is not null)
            {
                settings.Color.Brightness = dto.Color.Brightness;
                settings.Color.Contrast = dto.Color.Contrast;
                settings.Color.Gamma = dto.Color.Gamma;
                settings.Color.Saturation = dto.Color.Saturation;
                settings.Color.ColourVision = dto.Color.ColourVision;
            }

            if (dto.Zoom is not null)
            {
                settings.Zoom.Enabled = dto.Zoom.Enabled;
                settings.Zoom.Factor = dto.Zoom.Factor;
                settings.Zoom.Size = dto.Zoom.Size;
                settings.Zoom.OutlineThickness = dto.Zoom.OutlineThickness;
                settings.Zoom.Outline = dto.Zoom.Outline;
                settings.Zoom.Quality = dto.Zoom.Quality;
                settings.Zoom.TargetDeviceName = dto.Zoom.TargetDeviceName;
                settings.Zoom.HotkeyVk = dto.Zoom.HotkeyVk;
                settings.Zoom.HotkeyMode = dto.Zoom.HotkeyMode;
            }

            if (dto.Overlay is not null)
            {
                settings.Overlay.Enabled = dto.Overlay.Enabled;
                settings.Overlay.Style = dto.Overlay.Style;
                settings.Overlay.Size = dto.Overlay.Size;
                settings.Overlay.Thickness = dto.Overlay.Thickness;
                settings.Overlay.Gap = dto.Overlay.Gap;
                settings.Overlay.Opacity = dto.Overlay.Opacity;
                settings.Overlay.OffsetX = dto.Overlay.OffsetX;
                settings.Overlay.OffsetY = dto.Overlay.OffsetY;
                settings.Overlay.DotCenter = dto.Overlay.DotCenter;
                if (dto.Overlay.DotSize > 0)
                {
                    settings.Overlay.DotSize = dto.Overlay.DotSize;
                }
                settings.Overlay.Position = dto.Overlay.Position;
                settings.Overlay.CustomImagePath = dto.Overlay.CustomImagePath;
                settings.Overlay.HotkeyVk = dto.Overlay.HotkeyVk;
                settings.Overlay.HotkeyMode = dto.Overlay.HotkeyMode;
                settings.Overlay.SavedOffsetX = dto.Overlay.SavedOffsetX;
                settings.Overlay.SavedOffsetY = dto.Overlay.SavedOffsetY;
                if (OverlayOptions.TryParseHex(dto.Overlay.ColorHex, out var color))
                {
                    settings.Overlay.Color = color;
                }

                if (dto.Overlay.SavedPositions is { Count: > 0 })
                {
                    foreach (var item in dto.Overlay.SavedPositions)
                    {
                        if (string.IsNullOrWhiteSpace(item.Name))
                        {
                            continue;
                        }

                        settings.Overlay.SavedPositions.Add(new SavedOverlayPosition
                        {
                            Name = item.Name,
                            X = item.X,
                            Y = item.Y
                        });
                    }
                }
                else if (dto.Overlay.SavedOffsetX != 0 || dto.Overlay.SavedOffsetY != 0)
                {
                    settings.Overlay.SavedPositions.Add(new SavedOverlayPosition
                    {
                        Name = "Saved",
                        X = dto.Overlay.SavedOffsetX,
                        Y = dto.Overlay.SavedOffsetY
                    });
                }
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        IcyPaths.EnsureRoot();
        var dto = new SettingsDto
        {
            SelectedMonitorDeviceName = settings.SelectedMonitorDeviceName,
            ShowAllDisplayModes = settings.ShowAllDisplayModes,
            LoggingEnabled = settings.LoggingEnabled,
            ConsoleEnabled = settings.ConsoleEnabled,
            AutomaticStartup = settings.AutomaticStartup,
            MinimizeToTrayOnClose = settings.MinimizeToTrayOnClose,
            StartMinimized = settings.StartMinimized,
            Color = new ColorDto
            {
                Brightness = settings.Color.Brightness,
                Contrast = settings.Color.Contrast,
                Gamma = settings.Color.Gamma,
                Saturation = settings.Color.Saturation,
                ColourVision = settings.Color.ColourVision
            },
            Zoom = new ZoomDto
            {
                Enabled = settings.Zoom.Enabled,
                Factor = settings.Zoom.Factor,
                Size = settings.Zoom.Size,
                OutlineThickness = settings.Zoom.OutlineThickness,
                Outline = settings.Zoom.Outline,
                Quality = settings.Zoom.Quality,
                TargetDeviceName = settings.Zoom.TargetDeviceName,
                HotkeyVk = settings.Zoom.HotkeyVk,
                HotkeyMode = settings.Zoom.HotkeyMode
            },
            Overlay = new OverlayDto
            {
                Enabled = settings.Overlay.Enabled,
                Style = settings.Overlay.Style,
                ColorHex = settings.Overlay.ColorHex,
                Size = settings.Overlay.Size,
                Thickness = settings.Overlay.Thickness,
                Gap = settings.Overlay.Gap,
                Opacity = settings.Overlay.Opacity,
                OffsetX = settings.Overlay.OffsetX,
                OffsetY = settings.Overlay.OffsetY,
                DotCenter = settings.Overlay.DotCenter,
                DotSize = settings.Overlay.DotSize,
                Position = settings.Overlay.Position,
                CustomImagePath = settings.Overlay.CustomImagePath,
                HotkeyVk = settings.Overlay.HotkeyVk,
                HotkeyMode = settings.Overlay.HotkeyMode,
                SavedOffsetX = settings.Overlay.SavedOffsetX,
                SavedOffsetY = settings.Overlay.SavedOffsetY,
                SavedPositions = settings.Overlay.SavedPositions
                    .Select(p => new SavedPositionDto { Name = p.Name, X = p.X, Y = p.Y })
                    .ToList()
            }
        };

        File.WriteAllText(IcyPaths.SettingsFile, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private sealed class SettingsDto
    {
        public string? SelectedMonitorDeviceName { get; set; }
        public bool ShowAllDisplayModes { get; set; }
        public bool LoggingEnabled { get; set; }
        public bool ConsoleEnabled { get; set; }
        public bool AutomaticStartup { get; set; }
        public bool MinimizeToTrayOnClose { get; set; }
        public bool StartMinimized { get; set; }
        public ColorDto? Color { get; set; }
        public OverlayDto? Overlay { get; set; }
        public ZoomDto? Zoom { get; set; }
    }

    private sealed class ColorDto
    {
        public double Brightness { get; set; }
        public double Contrast { get; set; }
        public double Gamma { get; set; }
        public double Saturation { get; set; }
        public ColourVisionMode ColourVision { get; set; }
    }

    private sealed class OverlayDto
    {
        public bool Enabled { get; set; }
        public CrosshairStyle Style { get; set; }
        public string ColorHex { get; set; } = "#7EC8E3";
        public double Size { get; set; }
        public double Thickness { get; set; }
        public double Gap { get; set; }
        public double Opacity { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public bool DotCenter { get; set; }
        public double DotSize { get; set; }
        public OverlayPosition Position { get; set; }
        public string? CustomImagePath { get; set; }
        public int HotkeyVk { get; set; }
        public OverlayHotkeyMode HotkeyMode { get; set; }
        public double SavedOffsetX { get; set; }
        public double SavedOffsetY { get; set; }
        public List<SavedPositionDto> SavedPositions { get; set; } = [];
    }

    private sealed class ZoomDto
    {
        public bool Enabled { get; set; }
        public double Factor { get; set; } = 2.5;
        public double Size { get; set; } = 220;
        public double OutlineThickness { get; set; } = 4;
        public ZoomOutline Outline { get; set; } = ZoomOutline.White;
        public ZoomQuality Quality { get; set; } = ZoomQuality.Balanced;
        public string? TargetDeviceName { get; set; }
        public int HotkeyVk { get; set; }
        public OverlayHotkeyMode HotkeyMode { get; set; }
    }

    private sealed class SavedPositionDto
    {
        public string Name { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
    }
}
