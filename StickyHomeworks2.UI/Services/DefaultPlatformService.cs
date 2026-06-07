using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using StickyHomeworks2.Services;

namespace StickyHomeworks2.Services;

public class DefaultPlatformService : IPlatformService
{
    private static void LogNotImplemented(string methodName)
    {
        Debug.WriteLine($"[DefaultPlatformService] {methodName}() called — using stub implementation (no-op). Register a concrete platform service for full functionality.");
    }

    public void SetAlwaysOnBottom(IntPtr handle, bool isBottom)
    {
        LogNotImplemented(nameof(SetAlwaysOnBottom));
    }

    public bool IsForegroundFullScreen()
    {
        LogNotImplemented(nameof(IsForegroundFullScreen));
        return false;
    }

    public async Task<WindowInfo[]> GetAllWindowsAsync()
    {
        LogNotImplemented(nameof(GetAllWindowsAsync));
        return await Task.FromResult(Array.Empty<WindowInfo>());
    }

    public void RestartApplication()
    {
        throw new PlatformNotSupportedException($"{nameof(RestartApplication)} requires a platform-specific implementation.");
    }

    public string GetAppDataPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "StickyHomeworks2");
    }

    public void OpenInSystem(string pathOrUrl)
    {
        throw new PlatformNotSupportedException($"{nameof(OpenInSystem)} requires a platform-specific implementation.");
    }

    public async Task CopyToClipboardAsync(string text)
    {
        LogNotImplemented(nameof(CopyToClipboardAsync));
        await Task.CompletedTask;
    }

    public async Task<string?> FromClipboardAsync()
    {
        LogNotImplemented(nameof(FromClipboardAsync));
        return await Task.FromResult<string?>(null);
    }

    public async Task<string[]?> ShowOpenFileDialogAsync(string? filter)
    {
        LogNotImplemented(nameof(ShowOpenFileDialogAsync));
        return await Task.FromResult<string[]?>(null);
    }

    public async Task<string?> ShowFolderPickerDialogAsync()
    {
        LogNotImplemented(nameof(ShowFolderPickerDialogAsync));
        return await Task.FromResult<string?>(null);
    }

    public void ShowNotification(string title, string message)
    {
        LogNotImplemented(nameof(ShowNotification));
    }
}
