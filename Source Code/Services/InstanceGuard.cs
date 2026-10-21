using System.Threading;

namespace ICY.Services;

/// <summary>
/// Keeps one ICY process. A second launch asks the first window to show.
/// </summary>
public sealed class InstanceGuard : IDisposable
{
    private const string MutexName = @"Local\ICY.SingleInstance";
    private const string ShowName = @"Local\ICY.ShowMainWindow";

    private readonly Mutex? _mutex;
    private readonly EventWaitHandle? _show;
    private readonly Action _onShow;
    private readonly Thread? _listener;
    private volatile bool _run;

    public bool IsPrimary { get; }

    public InstanceGuard(Action onShow)
    {
        _onShow = onShow;
        _mutex = new Mutex(true, MutexName, out bool created);
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, ShowName);
        IsPrimary = created;

        if (!created)
        {
            _show.Set();
            return;
        }

        _run = true;
        _listener = new Thread(Listen)
        {
            IsBackground = true,
            Name = "ICY.Instance"
        };
        _listener.Start();
    }

    public void Dispose()
    {
        _run = false;
        try
        {
            _show?.Set();
        }
        catch
        {
            // Ignore a handle already closed during shutdown.
        }

        _show?.Dispose();
        _mutex?.Dispose();
    }

    private void Listen()
    {
        while (_run)
        {
            if (_show is null || !_show.WaitOne(400) || !_run)
            {
                continue;
            }

            try
            {
                _onShow();
            }
            catch
            {
                // The window may not exist yet during early startup.
            }
        }
    }
}
