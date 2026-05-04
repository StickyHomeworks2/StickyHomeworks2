using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StickyHomeworks2.Views;

/// <summary>
/// 打开外部链接前的确认对话框。
/// </summary>
public partial class ConfirmLinkWindow : Window
{
    public bool IsConfirmed { get; private set; }

    public ConfirmLinkWindow(string linkUri, string linkText)
    {
        InitializeComponent();
        LinkUriTextBox.Text = linkUri ?? string.Empty;
        var display = (linkText ?? string.Empty).Trim();
        LinkTextTextBlock.Text = display;
        LinkTextPanel.Visibility = string.IsNullOrEmpty(display) ? Visibility.Collapsed : Visibility.Visible;
        IsConfirmed = false;
    }

    private void LinkUriTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.SelectAll();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close();
    }
}
