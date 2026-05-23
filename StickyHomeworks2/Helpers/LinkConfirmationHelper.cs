using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Extensions.Logging;
using StickyHomeworks2.Views;

namespace StickyHomeworks2.Helpers;

public static class LinkConfirmationHelper
{
    private static ILogger? _logger;

    public static void SetLogger(ILogger logger) => _logger = logger;

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
            _logger?.LogInformation("打开外部链接: {Uri}", linkUri);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "打开链接失败: {Uri}", linkUri);
            MessageBox.Show($"无法打开链接: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
