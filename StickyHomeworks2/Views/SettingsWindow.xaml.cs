using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ClassIsland.Services;
using ElysiaFramework;
using ElysiaFramework.Controls;
using Markdig;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;
using StickyHomeworks2.Helpers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace StickyHomeworks.Views;
/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : MyWindow
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string Url { get; set; }
        public long Size { get; set; }
        public string Changelog { get; set; }
        public string Sha256 { get; set; }
        public string ReleaseDate { get; set; }
    }

    public SettingsViewModel ViewModel
    {
        get;
        set;
    } = new();

    public Settings Settings
    {
        get;
        set;
    } = new();

    public bool IsOpened
    {
        get;
        set;
    } = false;

    public WallpaperPickingService WallpaperPickingService { get; }

    private CancellationTokenSource _cts;
    private string _savePath;
    private const string UpdateInfoUrl = "http://eb48d3a3.xy.proaa.top/latest.json";
    private const string LocalAppData = "StickyHomeworks2Updater";

    public SettingsWindow(WallpaperPickingService wallpaperPickingService,
        SettingsService settingsService)
    {
        WallpaperPickingService = wallpaperPickingService;

        InitializeComponent();
        DataContext = this;
        Settings = settingsService.Settings;
        settingsService.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "Settings")
            {
                settingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
                Settings = settingsService.Settings;
            }
        };
        var style = (Style)FindResource("NotificationsListBoxItemStyle");
        //style.Setters.Add(new EventSetter(ListBoxItem.MouseDoubleClickEvent, new System.Windows.Input.MouseEventHandler(EventSetter_OnHandler)));

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        _savePath = System.IO.Path.Combine(exeDir, "UpdateTemp", "StickyHomeworks2.zip");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_savePath));

    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }


    protected override void OnInitialized(EventArgs e)
    {
        //RefreshMonitors();
        //var r = new StreamReader(Application.GetResourceStream(new Uri("/Assets/LICENSE.txt", UriKind.Relative))!.Stream);
        //ViewModel.License = r.ReadToEnd();
        base.OnInitialized(e);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        base.OnContentRendered(e);
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }

    private void SettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        AppEx.GetService<SettingsService>().SaveSettings();
        IsOpened = false;
    }

    private void ButtonCrash_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://learn.microsoft.com/zh-cn/appcenter/sdk/data-collected",
            UseShellExecute = true
        });
    }

    private void MyDrawerHost_OnDrawerClosing(object? sender, DrawerClosingEventArgs e)
    {
    }

    private void ButtonDebugToastText_OnClick(object sender, RoutedEventArgs e)
    {
        
    }


    private void ButtonDebugNetworkError_OnClick(object sender, RoutedEventArgs e)
    {
        //UpdateService.CurrentWorkingStatus = UpdateWorkingStatus.NetworkError;
    }


    private void OpenDrawer(string key)
    {
        MyDrawerHost.IsRightDrawerOpen = true;
        ViewModel.DrawerContent = FindResource(key);
    }

    private async Task<object?> ShowDialog(string key)
    {
        return await DialogHost.Show(FindResource(key), "SettingsWindow");
    }


    private void ButtonContributors_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ContributorsDrawer");
    }

    private void ButtonThirdPartyLibs_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ThirdPartyLibs");
    }

    private void AppIcon_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.AppIconClickCount >= 10)
        {
            Settings.IsDebugOptionsEnabled = true;
        }
    }

    private void ButtonCloseDebug_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.IsDebugOptionsEnabled = false;
        ViewModel.AppIconClickCount = 0;
    }

    private void MenuItemDebugScreenShot_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        await WallpaperPickingService.GetWallpaperAsync();
    }

    private async void ButtonBrowseWindows_OnClick(object sender, RoutedEventArgs e)
    {
        var w = new WindowsPicker(Settings.WallpaperClassName)
        {
            Owner = this,
        };
        var r = w.ShowDialog();
        Settings.WallpaperClassName = w.SelectedResult ?? "";
        if (r == true)
        {
            await WallpaperPickingService.GetWallpaperAsync();
        }
        GC.Collect();
    }

    private void MenuItemExperimentalSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupMenuOpened = false;
        OpenDrawer("ExperimentalSettings");
    }

    private async Task EditSubjectAsync(int index)
    {
        ViewModel.SubjectEditText = Settings.Subjects[index];
        var r = (string?)await ShowDialog("EditSubjectDialog");
        if (r == null) return;
        Settings.Subjects[index] = r;
    }

    private async Task EditTagAsync(int index)
    {
        ViewModel.TagEditText = Settings.Tags[index];
        var r = (string?)await ShowDialog("EditTagDialog");
        if (r == null) return;
        Settings.Tags[index] = r;
    }

    private async void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Subjects.Add("");
        await EditSubjectAsync(Settings.Subjects.Count - 1);
        var r = Settings.Subjects.Last();
        if (r == "")
        {
            Settings.Subjects.RemoveAt(Settings.Subjects.Count - 1);
        }
        else
        {
            ViewModel.SubjectSelectedIndex = Settings.Subjects.Count - 1;
        }
    }

    private async void ButtonEditSubject_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SubjectSelectedIndex == -1)
        {
            return;
        }
        await EditSubjectAsync(ViewModel.SubjectSelectedIndex);
    }

    private void ButtonDeleteSubject_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SubjectSelectedIndex == -1)
        {
            return;
        }
        Settings.Subjects.RemoveAt(ViewModel.SubjectSelectedIndex);
    }

    private async void ButtonAddTag_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Tags.Add("");
        await EditTagAsync(Settings.Tags.Count - 1);
        var r = Settings.Tags.Last();
        if (r == "")
        {
            Settings.Tags.RemoveAt(Settings.Tags.Count - 1);
        }
        else
        {
            ViewModel.TagSelectedIndex = Settings.Tags.Count - 1;
        }
    }

    private async void ButtonEditTag_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TagSelectedIndex == -1)
        {
            return;
        }
        await EditTagAsync(ViewModel.TagSelectedIndex);
    }

    private void ButtonDeleteTag_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TagSelectedIndex == -1)
        {
            return;
        }
        Settings.Tags.RemoveAt(ViewModel.TagSelectedIndex);
    }

    private void MenuItemTestHomeworkEditWindow_OnClick(object sender, RoutedEventArgs e)
    {
        AppEx.GetService<HomeworkEditWindow>().Show();
    }

    //<<<更新逻辑:开始>>>

    private async Task CheckForUpdatesAsync()
    {
        if (!CheckUpdatesButton.IsEnabled) return;                
        var currentVersion = GetCurrentVersion();

        CancelInstallUpdatesButton.Visibility = Visibility.Collapsed;
        DownloadUpdatesButton.Visibility = Visibility.Collapsed;
        InstallUpdatesButton.Visibility = Visibility.Collapsed;
        CheckUpdatesButton.IsEnabled = false;
        UpdateStatusTextBlock.Text = "正在检查更新...";
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.IsIndeterminate = true;
        UpdateIcon.Kind = PackIconKind.Update;
        await Task.Delay(1000);

        try
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var updateInfo = await client.GetFromJsonAsync<UpdateInfo>("https://eb48d3a3.xy.proaa.top/latest.json");



                if (updateInfo == null )
                {
                    versionStatusTextBlock.Text = $"当前版本：{currentVersion}";
                    UpdateStatusTextBlock.Text = $"检查失败";
                    DownloadProgress.IsIndeterminate = false;
                    DownloadProgress.Foreground = new SolidColorBrush(Colors.Red);
                    DownloadUpdatesButton.Visibility = Visibility.Collapsed;
                    return;
                }

               
                if (IsNewerVersion(currentVersion, updateInfo.Version))
                {
                    versionStatusTextBlock.Text = $"有新版本：{updateInfo.Version}";
                    UpdateStatusTextBlock.Text = $"更新可用";
                    DownloadProgress.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E88E5"));
                    DownloadProgress.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBDEFB"));
                    UpdateIcon.Kind = PackIconKind.Upload;
                    DownloadUpdatesButton.Tag = updateInfo.Url;
                    DownloadUpdatesButton.Visibility = Visibility.Visible;
                    CheckUpdatesButton.Visibility = Visibility.Visible;
                    DownloadUpdatesButton.IsEnabled = true;
                    CancelDownloadUpdateButton.IsEnabled = true;
                    CheckUpdatesButton.IsEnabled = true;
                    LoadTabs(); 
                }
                
                else
                {
                    DownloadProgress.IsIndeterminate = false;
                    versionStatusTextBlock.Text = $"当前版本：{currentVersion}";
                    UpdateStatusTextBlock.Text = $"已是最新版本";
                    UpdateIcon.Kind = PackIconKind.Update;
                    CheckUpdatesButton.Visibility = Visibility.Visible;
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    DownloadUpdatesButton.Visibility = Visibility.Collapsed; 
                    DownloadProgress.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E88E5"));
                    DownloadProgress.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBDEFB"));
                }
            }
        }
        catch (Exception ex)
        {
            
            versionStatusTextBlock.Text = "原因：" + ex.Message;
            UpdateStatusTextBlock.Text = $"检查失败";
            UpdateIcon.Kind = PackIconKind.Update;
            DownloadProgress.IsIndeterminate = false;
            DownloadProgress.Foreground = new SolidColorBrush(Colors.Red);
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }
     
    private async Task DownloadUpdatesAsync(object sender)
    {
        if (!DownloadUpdatesButton.IsEnabled) return;
        var downloadUrl = (string)((System.Windows.Controls.Button)sender).Tag;

        CheckUpdatesButton.IsEnabled = false;
        DownloadUpdatesButton.IsEnabled = false;
        CheckUpdatesButton.Visibility = Visibility.Visible;
        DownloadUpdatesButton.Visibility = Visibility.Collapsed;
        CancelDownloadUpdateButton.Visibility = Visibility.Visible;
        DownloadProgress.Visibility = Visibility.Visible;
        versionStatusTextBlock.Text = "即将开始下载...";
        await Task.Delay(250);
        _cts = new CancellationTokenSource();

        try
        {
            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(_savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read, _cts.Token);
                            totalRead += read;

                            Dispatcher.Invoke(() =>
                            {
                                if (totalBytes != -1)
                                {
                                    DownloadProgress.IsIndeterminate = false;
                                    var progress = (double)totalRead / totalBytes;
                                    DownloadProgress.Value = progress * 100;
                                    UpdateStatusTextBlock.Text = $"下载中...";
                                    versionStatusTextBlock.Text = $" {FormatSize(totalRead)}/{FormatSize(totalBytes)}";
                                }
                            });
                        }

                        await fileStream.FlushAsync();
                    }
                }

                versionStatusTextBlock.Text = "下载完成！";
                UpdateStatusTextBlock.Text = $"重启以应用更新";
                UpdateIcon.Kind = PackIconKind.MonitorArrowDownVariant;
                DownloadProgress.IsIndeterminate = true;
                CheckUpdatesButton.IsEnabled = true;
                CheckUpdatesButton.Visibility = Visibility.Collapsed;
                CancelInstallUpdatesButton.Visibility = Visibility.Visible;
                InstallUpdatesButton.Visibility = Visibility.Visible;
            }
        }
        catch (OperationCanceledException)
        {
            versionStatusTextBlock.Text = "下载已取消。";
            if (System.IO.File.Exists(_savePath))
                System.IO.File.Delete(_savePath);
            DownloadProgress.IsIndeterminate = true;
            DownloadUpdatesButton.IsEnabled = false;
            CancelDownloadUpdateButton.IsEnabled = false;
            CancelDownloadUpdateButton.Visibility = Visibility.Visible;
            CheckUpdatesButton.IsEnabled = true;
            DownloadProgress.Foreground = new SolidColorBrush(Colors.Red);
            UpdateIcon.Kind = PackIconKind.UploadOff;
            await Task.Delay(500);
            await CheckForUpdatesAsync();

        }
        catch (Exception ex)
        {
            UpdateStatusTextBlock.Text = $"下载失败";
            versionStatusTextBlock.Text = "原因：" + ex.Message;
            DownloadProgress.Foreground = new SolidColorBrush(Colors.Red);
            CheckUpdatesButton.Visibility = Visibility.Visible;
            CheckUpdatesButton.IsEnabled = true;
            UpdateIcon.Kind = PackIconKind.UploadOff;
        }
        finally
        {
            CancelDownloadUpdateButton.Visibility = Visibility.Collapsed;
        }

    }

    private async Task ExtractAndLaunchUpdaterAsync(string zipFilePath)
    {
        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException("更新包未找到。", zipFilePath);

        var extractDir = Path.Combine(Path.GetTempPath(), LocalAppData);
        var updaterBatPath = Path.Combine(extractDir, "update.bat");
        var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
        var appDirectory = Path.GetDirectoryName(currentExePath);

        try
        {
            // 清理文件夹
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, true);
            Directory.CreateDirectory(extractDir);


            Dispatcher.Invoke(() =>
            {
                UpdateStatusTextBlock.Text = "正在解压...";
                versionStatusTextBlock.Text = "正在解压更新包...";
                DownloadProgress.IsIndeterminate = true;
            });

            UpdateStatusTextBlock.Text = "开始安装...";

            await Task.Run(() => ZipFile.ExtractToDirectory(zipFilePath, extractDir, overwriteFiles: true));

          
            var batContent = $@"
@echo off
chcp 65001 >nul
color 0a
title StickyHomeworks2 更新程序

echo.
echo 正在应用更新，请稍候...
echo 解压目录: {extractDir}
echo 主程序目录: {appDirectory}
echo.
echo 等待退出...
timeout /t 3 >nul


robocopy ""{extractDir}"" ""{appDirectory}"" /E /Z /R:3 /W:5 /V /LOG:""{Path.Combine(extractDir, "update.log")}"" 

:: robocopy 成功返回码为 0-7，失败为 >=8
if %errorlevel% geq 8 (
    echo.
    echo [!] 更新失败: %errorlevel%
    echo 请手动重启程序。
    timeout /t 10 >nul
    exit /b 1
)

echo.
echo [!] 更新已成功应用。
echo 正在启动主程序...


start "" """"{currentExePath}""


echo 清理临时文件...
if exist ""{extractDir}"" rd /s /q ""{extractDir}""
del ""%~f0""

exit /b 0
";

          
            await File.WriteAllTextAsync(updaterBatPath, batContent, new UTF8Encoding(true));

            Dispatcher.Invoke(() =>
            {
                UpdateStatusTextBlock.Text = "准备重启...";
                versionStatusTextBlock.Text = "即将重启并应用更新...";
            });


        
            var startInfo = new ProcessStartInfo
            {
                FileName = updaterBatPath,        
                UseShellExecute = true,
                WorkingDirectory = extractDir,
                CreateNoWindow = false,                  
                WindowStyle = ProcessWindowStyle.Normal,
                ErrorDialog = false
            };

            Process.Start(startInfo);

      
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {

            var logPath = Path.Combine(extractDir, "update_error.log");

            try
            {
                
                var logContent = $@"[更新失败] {DateTime.Now:yyyy-MM-dd HH:mm:ss}
错误信息: {ex.Message}
异常类型: {ex.GetType().FullName}

堆栈跟踪:
{ex.StackTrace}

{(ex.InnerException != null ? $@"内部异常:
{ex.InnerException.Message}
{ex.InnerException.StackTrace}" : "")}

临时目录: {extractDir}
";

                await File.WriteAllTextAsync(logPath, logContent, new UTF8Encoding(true));
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"无法写入日志: {logEx.Message}");

            }

            string message = $"更新失败 \n\n错误: {ex.Message}\n\n日志已保存至:\n{logPath}\n\n。";


            Dispatcher.Invoke(() =>
            {
            System.Windows.Forms.MessageBox.Show(
                    message,
                    "更新失败",
                (MessageBoxButtons)MessageBoxButton.OK,
                (MessageBoxIcon)MessageBoxImage.Error);
            });


            try
            {
                if (Directory.Exists(extractDir))
                {
                    Process.Start("explorer.exe", $"/select,\"{logPath}\"");
                }
            }
            catch { }


            System.Diagnostics.Debug.WriteLine($"更新失败: {ex.Message}");


        System.Windows.Application.Current.Shutdown();
        }
    }

    private string GetCurrentVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    private bool IsNewerVersion(string current, string latest)
    {
        return Version.TryParse(current, out var cur) &&
               Version.TryParse(latest, out var newVer) &&
               newVer > cur;
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private async void CancelDownloadUpdateButton_Onclick(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        UpdateIcon.Kind = PackIconKind.UploadOff;
        versionStatusTextBlock.Text = "正在取消...";
        UpdateStatusTextBlock.Text = $"请稍后...";
        await Task.Delay(150);
        await CheckForUpdatesAsync();

    }
    private async void DownloadUpdatesButton_Onclick(object sender, RoutedEventArgs e)
    {
        await DownloadUpdatesAsync(sender);
    }
   
    private async void CheckUpdatesButton_Onclick(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync();
    }

    private void UpdateAdvancedSettingsButton_Onclick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("UpdateAdvancedSettings");
    }

    private async void InstallUpdatesButton_Onclick(object sender, RoutedEventArgs e)
    {
        await ExtractAndLaunchUpdaterAsync(_savePath);
    }

    private async void CancelInstallUpdatesButton_Onclick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.Visibility = Visibility.Visible;
        await CheckForUpdatesAsync();
    }

    private async void LoadTabs()
    {
        string[] urls = new string[]
        {
                "https://eb48d3a3.xy.proaa.top/"
        };

        foreach (var url in urls)
        {
            string markdownContent = await GetMarkdownContentFromUrl(url);
            if (!string.IsNullOrEmpty(markdownContent))
            {
                string htmlContent = Markdown.ToHtml(markdownContent);
                AddTabItem(url, htmlContent);
            }
        }
    }

    private async Task<string> GetMarkdownContentFromUrl(string url)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"无法加载 {url}: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return null;
    }

    private void AddTabItem(string url, string content)
    {
        string tabName = System.IO.Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
        TabItem tabItem = new TabItem
        {
            Header = tabName,
            Content = new System.Windows.Controls.WebBrowser { Source = new Uri($"data:text/html,{Uri.EscapeDataString(content)}") }
        };
        tabControl.Items.Add(tabItem);
    }


    //<<<更新逻辑:结束>>>

    private void MultipleOpeningsText_OnClick(object sender, RoutedEventArgs e)
    {
        SingleInstanceWarning warningWindow = new();
        warningWindow.ShowDialog();
    }
    private void Github(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/StickyHomeworks2/StickyHomeworks2",
            UseShellExecute = true
        });
    }
    private void Issues(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/StickyHomeworks2/StickyHomeworks2/issues",
            UseShellExecute = true
        });
    }
}
