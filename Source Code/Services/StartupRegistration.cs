using System.IO;
using Microsoft.Win32;

namespace ICY.Services;

/// <summary>
/// Current-user Run key only. Does not write HKLM or scheduled tasks.
/// </summary>
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ICY";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null)
        {
            return;
        }

        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        string? path = ResolveExecutablePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        key.SetValue(ValueName, "\"" + path + "\"");
    }

    private static string? ResolveExecutablePath()
    {
        string? path = Environment.ProcessPath;
        if (IsIcyExecutable(path))
        {
            return path;
        }

        string fallback = Path.Combine(AppContext.BaseDirectory, "ICY.exe");
        return File.Exists(fallback) ? fallback : path;
    }

    private static bool IsIcyExecutable(string? path)
        => !string.IsNullOrWhiteSpace(path)
           && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
           && !path.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase);
}
