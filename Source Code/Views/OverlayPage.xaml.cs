using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ICY.ViewModels;

namespace ICY.Views;

public partial class OverlayPage : UserControl
{
    public OverlayPage()
    {
        InitializeComponent();
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !vm.IsCapturingHotkey)
        {
            return;
        }

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt)
        {
            key = e.Key == Key.System ? e.SystemKey : key;
        }

        vm.SetHotkey(KeyInterop.VirtualKeyFromKey(key));
        e.Handled = true;
    }

    private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !vm.IsCapturingHotkey)
        {
            return;
        }

        int vk = e.ChangedButton switch
        {
            MouseButton.Left => 1,
            MouseButton.Right => 2,
            MouseButton.Middle => 4,
            MouseButton.XButton1 => 5,
            MouseButton.XButton2 => 6,
            _ => 0
        };

        if (vk == 0)
        {
            return;
        }

        vm.SetHotkey(vk);
        e.Handled = true;
    }
}
