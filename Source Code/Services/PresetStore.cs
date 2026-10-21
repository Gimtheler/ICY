using System.IO;
using System.Text.Json;
using ICY.Models;

namespace ICY.Services;

public sealed class PresetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<ColorPreset> LoadAll()
    {
        var result = new List<ColorPreset>();
        try
        {
            if (!File.Exists(IcyPaths.PresetsFile))
            {
                SaveUserPresets(Array.Empty<ColorPreset>());
                return result;
            }

            string json = File.ReadAllText(IcyPaths.PresetsFile);
            var file = JsonSerializer.Deserialize<PresetFile>(json, JsonOptions);
            if (file?.Presets is null)
            {
                return result;
            }

            foreach (var preset in file.Presets)
            {
                if (string.IsNullOrWhiteSpace(preset.Name))
                {
                    continue;
                }

                preset.IsBuiltIn = false;
                if (result.Any(p => string.Equals(p.Name, preset.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(preset);
            }
        }
        catch
        {
            // Keep an empty list if the file is unreadable.
        }

        return result;
    }

    public void SaveUserPresets(IEnumerable<ColorPreset> presets)
    {
        IcyPaths.EnsureRoot();
        var file = new PresetFile
        {
            Presets = presets.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList()
        };
        foreach (var preset in file.Presets)
        {
            preset.IsBuiltIn = false;
        }

        File.WriteAllText(IcyPaths.PresetsFile, JsonSerializer.Serialize(file, JsonOptions));
    }

    public void Export(ColorPreset preset, string path)
    {
        var payload = new PresetFile
        {
            Presets = [Clone(preset)]
        };
        payload.Presets[0].IsBuiltIn = false;
        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOptions));
    }

    public List<ColorPreset> Import(string path)
    {
        string json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<PresetFile>(json, JsonOptions);
        var list = new List<ColorPreset>();
        if (file?.Presets is { Count: > 0 })
        {
            foreach (var preset in file.Presets)
            {
                if (string.IsNullOrWhiteSpace(preset.Name))
                {
                    continue;
                }

                preset.IsBuiltIn = false;
                list.Add(preset);
            }

            return list;
        }

        var single = JsonSerializer.Deserialize<ColorPreset>(json, JsonOptions);
        if (single is not null && !string.IsNullOrWhiteSpace(single.Name))
        {
            single.IsBuiltIn = false;
            list.Add(single);
        }

        return list;
    }

    private static ColorPreset Clone(ColorPreset preset)
        => new()
        {
            Name = preset.Name,
            IsBuiltIn = false,
            Brightness = preset.Brightness,
            Contrast = preset.Contrast,
            Gamma = preset.Gamma,
            Saturation = preset.Saturation,
            ColourVision = preset.ColourVision
        };

    private sealed class PresetFile
    {
        public List<ColorPreset> Presets { get; set; } = [];
    }
}
