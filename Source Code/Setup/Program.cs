using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ICY.Setup;

internal static class Program
{
    private const uint MbOk = 0;
    private const uint MbYesNo = 4;
    private const uint MbIconInformation = 0x40;
    private const uint IdYes = 6;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int MessageBoxW(nint hWnd, string text, string caption, uint type);

    [STAThread]
    private static void Main()
    {
        if (MessageBoxW(0, "Install ICY V1.0? This adds Application.exe for the current user. No admin is required.", "ICY V1.0", MbYesNo | MbIconInformation) != IdYes)
        {
            return;
        }

        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ICY");
            string app = Path.Combine(dir, "Application.exe");
            Directory.CreateDirectory(dir);

            using (var source = OpenPayload())
            using (var dest = File.Create(app))
            {
                source.CopyTo(dest);
            }

            CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "ICY.lnk"), app, dir);
            CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ICY.lnk"), app, dir);

            if (MessageBoxW(0, "Installed Application.exe.\n\nLaunch ICY now?", "ICY V1.0", MbYesNo | MbIconInformation) == IdYes)
            {
                Process.Start(new ProcessStartInfo { FileName = app, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBoxW(0, ex.Message, "ICY V1.0", MbOk);
        }
    }

    private static Stream OpenPayload()
    {
        var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream("Application.exe");
        if (embedded is not null)
        {
            return embedded;
        }

        string beside = Path.Combine(AppContext.BaseDirectory, "Application.exe");
        if (File.Exists(beside))
        {
            return File.OpenRead(beside);
        }

        throw new FileNotFoundException("Application.exe is missing from this package.");
    }

    private static void CreateShortcut(string linkPath, string target, string workDir)
    {
        string? folder = Path.GetDirectoryName(linkPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string ps = "$w = New-Object -ComObject WScript.Shell; " +
                    $"$l = $w.CreateShortcut('{Escape(linkPath)}'); " +
                    $"$l.TargetPath = '{Escape(target)}'; " +
                    $"$l.WorkingDirectory = '{Escape(workDir)}'; " +
                    "$l.Save()";
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -Command " + ps,
            UseShellExecute = false,
            CreateNoWindow = true
        })?.WaitForExit(15000);
    }

    private static string Escape(string path) => path.Replace("'", "''");
}
