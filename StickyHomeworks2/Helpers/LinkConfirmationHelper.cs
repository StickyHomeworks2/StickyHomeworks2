using System;
using System.Diagnostics;
using System.Windows;
using StickyHomeworks2.Views;

namespace StickyHomeworks2.Helpers;

public static class LinkConfirmationHelper
{
    public static void ConfirmAndOpenLink(string linkUri, string linkText)
    {
        var confirmWindow = new ConfirmLinkWindow(linkUri, linkText);
        if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
            confirmWindow.Owner = Application.Current.MainWindow;
        confirmWindow.ShowDialog();

        if (!confirmWindow.IsConfirmed)
            return;

        var psi = new ProcessStartInfo
        {
            FileName = linkUri,
            UseShellExecute = true
        };
        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
