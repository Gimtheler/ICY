using System.Runtime.InteropServices;
using System.Windows.Interop;
using ICY.Native;

namespace ICY.Views;

/// <summary>
/// Hosts the OS Magnifier child HWND (WC_MAGNIFIER) inside ICY's zoom window.
/// </summary>
public sealed class MagnifierHost : HwndHost
{
    private IntPtr _hwnd;

    public IntPtr MagnifierHandle => _hwnd;

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _hwnd = NativeMethods.CreateWindowEx(
            0,
            NativeMethods.WcMagnifier,
            string.Empty,
            NativeMethods.WsChild | NativeMethods.WsVisible | NativeMethods.WsClipSiblings,
            0,
            0,
            Math.Max(1, double.IsNaN(Width) ? 64 : (int)Width),
            Math.Max(1, double.IsNaN(Height) ? 64 : (int)Height),
            hwndParent.Handle,
            IntPtr.Zero,
            NativeMethods.GetModuleHandle(null),
            IntPtr.Zero);

        return new HandleRef(this, _hwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        if (hwnd.Handle != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(hwnd.Handle);
        }

        _hwnd = IntPtr.Zero;
    }

    public void ResizeLens(int width, int height)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.MoveWindow(_hwnd, 0, 0, Math.Max(1, width), Math.Max(1, height), true);
        ApplyCircle(width, height);
    }

    public void ApplyCircle(int width, int height)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        IntPtr region = NativeMethods.CreateEllipticRgn(0, 0, Math.Max(1, width), Math.Max(1, height));
        if (region != IntPtr.Zero)
        {
            NativeMethods.SetWindowRgn(_hwnd, region, true);
        }
    }

    public void SetZoom(double factor)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        var transform = MagTransform.Scale((float)factor);
        NativeMethods.MagSetWindowTransform(_hwnd, ref transform);
    }

    public void SetSource(int left, int top, int right, int bottom)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.MagSetWindowSource(_hwnd, new Rect
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom
        });
    }

    public void Exclude(params IntPtr[] hwnds)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        IntPtr[] list = hwnds.Where(h => h != IntPtr.Zero).Distinct().ToArray();
        if (list.Length == 0)
        {
            return;
        }

        NativeMethods.MagSetWindowFilterList(
            _hwnd,
            NativeMethods.MwFilterModeExclude,
            list.Length,
            list);
    }
}
