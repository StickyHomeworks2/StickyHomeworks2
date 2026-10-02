namespace StickyHomeworks.Core;

/// <summary>
/// Provides absolute paths for application-owned data files.
/// </summary>
public static class AppPaths
{
    public static string BaseDirectory { get; } = AppContext.BaseDirectory;

    public static string SettingsFile { get; } = Path.Combine(BaseDirectory, "Settings.json");

    public static string ProfileFile { get; } = Path.Combine(BaseDirectory, "Profile.json");

    public static string BackupDirectory { get; } = Path.Combine(BaseDirectory, "backups");

    public static string DatabaseFile { get; } = Path.Combine(BaseDirectory, "db", "app.db");
}
