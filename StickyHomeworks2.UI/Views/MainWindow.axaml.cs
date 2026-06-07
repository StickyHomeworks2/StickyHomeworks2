using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace StickyHomeworks2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<Popup>("MenuPopup") is { } popup)
        {
            popup.IsOpen = true;
        }
    }

    private void MenuItem_OnClick(object? sender, RoutedEventArgs e) { }

    private void RecoverExpired_OnClick(object? sender, RoutedEventArgs e) { }

    private void TimeMachine_OnClick(object? sender, RoutedEventArgs e) { }

    private void Settings_OnClick(object? sender, RoutedEventArgs e) { }

    private void Exit_OnClick(object? sender, RoutedEventArgs e) { }
}
