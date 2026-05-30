using System;
using System.Threading.Tasks;

namespace StickyHomeworks2.Services;

public interface IPlatformService
{
    void SetAlwaysOnBottom(IntPtr handle, bool isBottom);

    bool IsForegroundFullScreen();

    Task<WindowInfo[]> GetAllWindowsAsync();

    void RestartApplication();

    string GetAppDataPath();

    void OpenInSystem(string pathOrUrl);

    Task CopyToClipboardAsync(string text);

    Task<string?> FromClipboardAsync();

    Task<string[]?> ShowOpenFileDialogAsync(string? filter = null);

    Task<string?> ShowFolderPickerDialogAsync();

    void ShowNotification(string title, string message);
}

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
