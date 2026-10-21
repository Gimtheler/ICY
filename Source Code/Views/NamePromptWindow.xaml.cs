using System.Windows;

namespace ICY.Views;

public partial class NamePromptWindow : Window
{
    public NamePromptWindow(string title, string? suggested)
    {
        InitializeComponent();
        TitleText.Text = title;
        NameBox.Text = suggested ?? string.Empty;
        NameBox.SelectAll();
        Loaded += (_, _) => NameBox.Focus();
    }

    public string ResultName => NameBox.Text.Trim();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ResultName.Length == 0)
        {
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
