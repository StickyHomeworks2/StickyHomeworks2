using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace StickyHomeworks2.Updater;

/// <summary>
/// Main window for the updater
/// </summary>
public partial class MainWindow : Window
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ShellExecuteW(IntPtr hWnd, string? lpOperation, string lpFile, string? lpParameters, string? lpDirectory, int nShowCmd);

    private const int SW_SHOWNORMAL = 1;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(500); // Wait for UI to render
        await RunUpdateAsync();
    }

    private void UpdateProgress(int value, string status)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = value;
            PercentText.Text = $"{value}%";
            StatusText.Text = status;
        });
    }

    private void UpdateTitle(string title)
    {
        Dispatcher.Invoke(() => TitleText.Text = title);
    }

    private async Task RunUpdateAsync()
    {
        var args = App.Arguments;
        if (args == null)
        {
            ShowError("无效的参数");
            return;
        }

        var appDir = new DirectoryInfo(args.AppDir);
        var bakDir = new DirectoryInfo(Path.Combine(args.AppDir, ".update_bak"));
        var zipPath = new FileInfo(args.ZipPath);
        var logFile = new FileInfo(Path.Combine(args.AppDir, "update_error.log"));
        var unpackDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "StickyHomeworks2_Update_" + Guid.NewGuid().ToString("N")));

        try
        {
            // Step 1: Wait for parent process to exit
            UpdateProgress(5, "正在终止正在运行的实例...");

            if (args.ParentPid > 0)
            {
                ForceKillProcessTree(args.ParentPid);
                if (!WaitForProcessExit(args.ParentPid, 5000))
                {
                    throw new Exception("等待主进程退出超时");
                }
            }

            // Also kill any other StickyHomeworks2.exe just in case
            try
            {
                var processName = Path.GetFileNameWithoutExtension(appDir.FullName);
                foreach (var proc in Process.GetProcessesByName("StickyHomeworks2"))
                {
                    try { proc.Kill(); } catch { }
                }
            }
            catch { }

            await Task.Delay(1000);

            // Step 2: Backup current version
            UpdateProgress(15, "备份当前版本...");
            await Task.Run(() => BackupApp(appDir, bakDir));

            // Step 3: Extract update package
            UpdateProgress(30, "解压更新包...");
            await Task.Run(() =>
            {
                if (unpackDir.Exists)
                    unpackDir.Delete(true);
                unpackDir.Create();

                ZipFile.ExtractToDirectory(zipPath.FullName, unpackDir.FullName);
            });

            // Check if zip contains a single folder
            var actualUnpackDir = unpackDir;
            var extractedItems = unpackDir.GetFileSystemInfos();
            if (extractedItems.Length == 1 && extractedItems[0] is DirectoryInfo dir)
            {
                actualUnpackDir = dir;
            }

            // Step 4: Clean old files
            UpdateProgress(50, "清理旧文件...");
            await Task.Run(() =>
            {
                var exePath = Path.Combine(appDir.FullName, "StickyHomeworks2.exe");
                if (File.Exists(exePath))
                    File.Delete(exePath);

                var internalDir = new DirectoryInfo(Path.Combine(appDir.FullName, "_internal"));
                if (internalDir.Exists)
                {
                    foreach (var item in internalDir.GetFileSystemInfos())
                    {
                        if (item.Name.Equals("user", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (item is DirectoryInfo d)
                            d.Delete(true);
                        else
                            item.Delete();
                    }
                }
            });

            // Step 5: Apply new files
            UpdateProgress(70, "应用新文件...");
            await Task.Run(() =>
            {
                var exePath = Path.Combine(appDir.FullName, "StickyHomeworks2.exe");
                var newExe = Path.Combine(actualUnpackDir.FullName, "StickyHomeworks2.exe");
                if (File.Exists(newExe))
                    File.Copy(newExe, exePath, true);

                var newInternal = new DirectoryInfo(Path.Combine(actualUnpackDir.FullName, "_internal"));
                var internalDir = new DirectoryInfo(Path.Combine(appDir.FullName, "_internal"));
                if (!internalDir.Exists)
                    internalDir.Create();

                if (newInternal.Exists)
                {
                    CopyDirectory(newInternal, internalDir, "user");
                }
            });

            // Step 6: Write version file
            UpdateProgress(90, "完成配置...");
            var versionFile = new FileInfo(Path.Combine(appDir.FullName, "_internal", ".version"));
            if (!versionFile.Directory!.Exists)
                versionFile.Directory.Create();
            await File.WriteAllTextAsync(versionFile.FullName, args.Version);

            // Step 7: Launch new version
            UpdateProgress(100, "正在启动新版本...");

            // Cleanup UpdateTemp directory
            try
            {
                var updateTempDir = new DirectoryInfo(Path.Combine(args.AppDir, "UpdateTemp"));
                if (updateTempDir.Exists)
                    updateTempDir.Delete(true);
            }
            catch { }

            var finalExe = Path.Combine(appDir.FullName, "StickyHomeworks2.exe");
            if (File.Exists(finalExe))
            {
                ShellExecuteW(IntPtr.Zero, "open", finalExe, null, appDir.FullName, SW_SHOWNORMAL);
            }

            // Close updater
            Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            // Log error
            try
            {
                await File.AppendAllTextAsync(logFile.FullName,
                    $"[{DateTime.Now}] Update failed: {ex}\n");
            }
            catch { }

            // Restore backup
            UpdateProgress(0, "更新失败，正在回滚...");
            await Task.Run(() => RestoreBackup(appDir, bakDir));

            ShowError($"更新失败: {ex.Message}");

            // Try to restart old version
            var oldExe = Path.Combine(appDir.FullName, "StickyHomeworks2.exe");
            if (File.Exists(oldExe))
            {
                ShellExecuteW(IntPtr.Zero, "open", oldExe, null, appDir.FullName, SW_SHOWNORMAL);
            }

            await Task.Delay(3000);
            Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        finally
        {
            // Cleanup unpack directory
            try
            {
                if (unpackDir.Exists)
                    unpackDir.Delete(true);
            }
            catch { }
        }
    }

    private void ShowError(string message)
    {
        Dispatcher.Invoke(() =>
        {
            TitleText.Text = "更新失败";
            ProgressBar.Value = 0;
            PercentText.Text = "0%";
            ProgressBar.Foreground = TryFindResource("MaterialDesignValidationErrorBrush") as Brush ?? Brushes.IndianRed;
            StatusText.Text = message;
        });
    }

    #region Process Management

    private static bool WaitForProcessExit(int pid, int timeoutMs)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            if (process.HasExited)
                return true;

            return process.WaitForExit(timeoutMs);
        }
        catch
        {
            return true; // Process not found, treat as exited
        }
    }

    private static void ForceKillProcessTree(int pid)
    {
        if (pid <= 0) return;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/PID {pid} /T /F",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Process.Start(startInfo)?.WaitForExit();
        }
        catch { }
    }

    #endregion

    #region Backup and Restore

    private static void BackupApp(DirectoryInfo appDir, DirectoryInfo bakDir)
    {
        if (bakDir.Exists)
            bakDir.Delete(true);
        bakDir.Create();

        // Backup executable
        var exePath = Path.Combine(appDir.FullName, "StickyHomeworks2.exe");
        if (File.Exists(exePath))
            File.Copy(exePath, Path.Combine(bakDir.FullName, "StickyHomeworks2.exe"), true);

        // Backup _internal
        var internalDir = new DirectoryInfo(Path.Combine(appDir.FullName, "_internal"));
        if (internalDir.Exists)
        {
            var bakInternal = new DirectoryInfo(Path.Combine(bakDir.FullName, "_internal"));
            CopyDirectory(internalDir, bakInternal);
        }
    }

    private static void RestoreBackup(DirectoryInfo appDir, DirectoryInfo bakDir)
    {
        if (!bakDir.Exists) return;

        try
        {
            // Restore executable
            var bakExe = Path.Combine(bakDir.FullName, "StickyHomeworks2.exe");
            if (File.Exists(bakExe))
                File.Copy(bakExe, Path.Combine(appDir.FullName, "StickyHomeworks2.exe"), true);

            // Restore _internal
            var bakInternal = new DirectoryInfo(Path.Combine(bakDir.FullName, "_internal"));
            var internalDir = new DirectoryInfo(Path.Combine(appDir.FullName, "_internal"));
            if (bakInternal.Exists)
            {
                if (!internalDir.Exists)
                    internalDir.Create();
                CopyDirectory(bakInternal, internalDir);
            }
        }
        catch { }
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target, string? excludeDir = null)
    {
        if (!target.Exists)
            target.Create();

        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        foreach (var dir in source.GetDirectories())
        {
            if (!string.IsNullOrEmpty(excludeDir) &&
                dir.Name.Equals(excludeDir, StringComparison.OrdinalIgnoreCase))
                continue;

            var newDir = new DirectoryInfo(Path.Combine(target.FullName, dir.Name));
            CopyDirectory(dir, newDir, excludeDir);
        }
    }

    #endregion
}
