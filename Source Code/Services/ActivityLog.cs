using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace ICY.Services;

public sealed class ActivityLog
{
    private readonly object _gate = new();
    private readonly int _cap = 200;

    public ObservableCollection<string> Lines { get; } = new();

    public bool LoggingEnabled { get; set; }

    public bool ConsoleEnabled { get; set; }

    public void Info(string message)
    {
        if (!LoggingEnabled && !ConsoleEnabled)
        {
            return;
        }

        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}";

        if (ConsoleEnabled)
        {
            try
            {
                Console.WriteLine(line);
            }
            catch
            {
                // Ignore console write failures.
            }
        }

        if (!LoggingEnabled)
        {
            return;
        }

        void Add()
        {
            Lines.Insert(0, line);
            while (Lines.Count > _cap)
            {
                Lines.RemoveAt(Lines.Count - 1);
            }
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Add);
        }
        else
        {
            Add();
        }

        try
        {
            IcyPaths.EnsureRoot();
            lock (_gate)
            {
                File.AppendAllText(IcyPaths.ActivityLogFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never break display changes.
        }
    }
}
