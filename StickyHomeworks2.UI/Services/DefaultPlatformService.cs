using System;
using System.IO;
using System.Threading.Tasks;
using StickyHomeworks2.Services;

namespace StickyHomeworks2.Services;

public class DefaultPlatformService : IPlatformService
{
    public void SetAlwaysOnBottom(IntPtr handle, bool isBottom)
    {
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
        throw new PlatformNotSupportedException();
    }

    public string GetAppDataPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "StickyHomeworks2");
    }

    public void OpenInSystem(string pathOrUrl)
    {
        throw new PlatformNotSupportedException();
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
