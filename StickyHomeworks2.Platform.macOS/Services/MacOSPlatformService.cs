using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using StickyHomeworks2.Services;

namespace StickyHomeworks2.Platform.macOS.Services;

public class MacOSPlatformService : IPlatformService
{
    public void SetAlwaysOnBottom(IntPtr handle, bool isBottom)
    {
        throw new PlatformNotSupportedException("Stay-bottom limited support on macOS");
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
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "Application Support", "StickyHomeworks2"
        );
    }

    public void OpenInSystem(string pathOrUrl)
    {
        Process.Start("open", pathOrUrl);
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
        Process.Start("osascript", $"-e 'display notification \"{message}\" with title \"{title}\"'");
    }
}
