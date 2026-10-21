using System.IO;
using System.Runtime.InteropServices;

namespace ICY.Services;

/// <summary>
/// Optional debug console. Off by default so ICY does not allocate a console or
/// keep a log sink running unless the user turns it on in Settings.
/// </summary>
internal static class DebugConsole
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    public static bool IsOpen => GetConsoleWindow() != IntPtr.Zero;

    public static void SetVisible(bool visible)
    {
        if (visible)
        {
            if (IsOpen)
            {
                return;
            }

            if (!AllocConsole())
            {
                return;
            }

            Console.Title = "ICY";
            try
            {
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
            catch
            {
                // Console handle can fail if the process already had a redirected stream.
            }
        }
        else if (IsOpen)
        {
            FreeConsole();
        }
    }
}
