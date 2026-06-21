using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace StickyHomeworks2.Updater;

/// <summary>
/// Updater application entry point
/// </summary>
public partial class App : Application
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ShellExecuteW(IntPtr hWnd, string? lpOperation, string lpFile, string? lpParameters, string? lpDirectory, int nShowCmd);

    [DllImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsUserAnAdmin();

    private const int SW_SHOWNORMAL = 1;

    public static UpdaterArguments? Arguments { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Parse command line arguments
        Arguments = ParseArguments(e.Args);

        if (Arguments == null)
        {
            MessageBox.Show("Invalid arguments. Usage: StickyHomeworks2.Updater.exe --zip <path> --version <version> --app-dir <dir> --parent-pid <pid>",
                "Updater Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Check if running as admin
        if (!IsUserAnAdmin())
        {
            // Request admin privileges and restart
            RequestAdminPrivileges();
            Shutdown();
            return;
        }

        // We are admin, show main window and start update
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private UpdaterArguments? ParseArguments(string[] args)
    {
        string? zip = null;
        string? version = null;
        string? appDir = null;
        int parentPid = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--zip":
                    if (i + 1 < args.Length) zip = args[++i];
                    break;
                case "--version":
                    if (i + 1 < args.Length) version = args[++i];
                    break;
                case "--app-dir":
                    if (i + 1 < args.Length) appDir = args[++i];
                    break;
                case "--parent-pid":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int pid))
                        parentPid = pid;
                    break;
            }
        }

        if (string.IsNullOrEmpty(zip) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(appDir))
            return null;

        return new UpdaterArguments(zip, version, appDir, parentPid);
    }

    private void RequestAdminPrivileges()
    {
        if (Arguments == null) return;

        // Build command line parameters
        var args = $"--zip \"{Arguments.ZipPath}\" --version \"{Arguments.Version}\" --app-dir \"{Arguments.AppDir}\" --parent-pid {Arguments.ParentPid}";

        // Use ShellExecuteW to request elevation
        ShellExecuteW(IntPtr.Zero, "runas", Process.GetCurrentProcess().MainModule?.FileName ?? "StickyHomeworks2.Updater.exe", args, null, SW_SHOWNORMAL);
    }
}

/// <summary>
/// Command line arguments for the updater
/// </summary>
public record UpdaterArguments(string ZipPath, string Version, string AppDir, int ParentPid);
