using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using StickyHomeworks2.Services;

namespace StickyHomeworks2.Platform.Windows.Services;

public class WindowsPlatformService : IPlatformService
{
    private const int HWND_BOTTOM = 1;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_SHOWWINDOW = 0x0040;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy,
        uint uFlags);

    public void SetAlwaysOnBottom(IntPtr handle, bool isBottom)
    {
        if (isBottom)
        {
            SetWindowPos(handle, new IntPtr(HWND_BOTTOM),
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }

    public bool IsForegroundFullScreen()
    {
        return false;
    }

    public async Task<WindowInfo[]> GetAllWindowsAsync()
    {
        return await Task.FromResult(Array.Empty<WindowInfo>());
    }

    public void RestartApplication()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            UseShellExecute = true
        });
        Environment.Exit(0);
    }

    public string GetAppDataPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "StickyHomeworks2");
    }

    public void OpenInSystem(string pathOrUrl)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = pathOrUrl,
            UseShellExecute = true
        });
    }

    public async Task CopyToClipboardAsync(string text)
    {
        await Task.CompletedTask;
    }

    public async Task<string?> FromClipboardAsync()
    {
        return await Task.FromResult<string?>(null);
    }

    public async Task<string[]?> ShowOpenFileDialogAsync(string? filter)
    {
        return await Task.FromResult<string[]?>(null);
    }

    public async Task<string?> ShowFolderPickerDialogAsync()
    {
        return await Task.FromResult<string?>(null);
    }

    public void ShowNotification(string title, string message)
    {
    }
}
