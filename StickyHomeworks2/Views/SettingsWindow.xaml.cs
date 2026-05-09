using ClassIsland.Services;
using ElysiaFramework;
using ElysiaFramework.Controls;
using Markdig;
using Markdig.Wpf;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;
using StickyHomeworks2.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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
    public ClassIslandIpcService ClassIslandIpcService { get; }

    private readonly SettingsService _settingsService;
    private readonly PropertyChangedEventHandler _settingsServiceRootPropertyChanged;

    private CancellationTokenSource _cts;
    private string _savePath;
    private string _updateVersion;
    private const string UpdateInfoUrl = "https://api.classisband.xyz/api/latest.json";
    private const string LocalAppData = "StickyHomeworks2Updater";
    private HttpClient? _httpClient;
    private const string ChangelogUrl = "https://api.classisband.xyz/api/changelog.md";

    private readonly ObservableCollection<string> _homeworkTemplateCommonBookKeys = new();
    private readonly ObservableCollection<string> _homeworkTemplateSubjectBookKeys = new();
    private DispatcherTimer? _homeworkTemplatePersistDebounceTimer;

    public SettingsWindow(WallpaperPickingService wallpaperPickingService,
        SettingsService settingsService,
        ClassIslandIpcService classIslandIpcService)
    {
        WallpaperPickingService = wallpaperPickingService;
        ClassIslandIpcService = classIslandIpcService;
        _settingsService = settingsService;
        _settingsServiceRootPropertyChanged = SettingsServiceOnRootPropertyChanged;

        InitializeComponent();
        DataContext = this;
        Settings = settingsService.Settings;
        LbHomeworkTemplateCommonBooks.ItemsSource = _homeworkTemplateCommonBookKeys;
        LbHomeworkTemplateSubjectBooks.ItemsSource = _homeworkTemplateSubjectBookKeys;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        _settingsService.PropertyChanged += _settingsServiceRootPropertyChanged;
        var style = (Style)FindResource("NotificationsListBoxItemStyle");
        //style.Setters.Add(new EventSetter(ListBoxItem.MouseDoubleClickEvent, new System.Windows.Input.MouseEventHandler(EventSetter_OnHandler)));

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        _savePath = System.IO.Path.Combine(exeDir, "UpdateTemp", "StickyHomeworks2.zip");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_savePath));

        EnsureHttpClient();
    }

    private void EnsureHttpClient()
    {
        if (_httpClient != null)
            return;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
    }

    private void SettingsServiceOnRootPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(SettingsService.Settings))
            return;
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
        Settings = _settingsService.Settings;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        Settings.PropertyChanged -= SettingsOnPropertyChanged;
        _settingsService.PropertyChanged -= _settingsServiceRootPropertyChanged;
        _httpClient?.Dispose();
        _httpClient = null;
        base.OnClosed(e);
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

    private void DocumentViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        var viewer = sender as System.Windows.Controls.FlowDocumentScrollViewer;
        if (viewer == null) return;
        
        // 使用 VisualTreeHelper 查找内部 ScrollViewer
        var scrollViewer = FindVisualChild<ScrollViewer>(viewer);
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
        }
    }
    
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is T found)
                return found;
            
            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private void SettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        _homeworkTemplatePersistDebounceTimer?.Stop();
        _homeworkTemplatePersistDebounceTimer = null;
        PersistHomeworkTemplate();
        Hide();
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

    private async void ButtonRefreshClassIslandSubjects_OnClick(object sender, RoutedEventArgs e)
    {
        var subjects = await ClassIslandIpcService.GetSubjectsAsync();
        if (subjects.Count == 0)
        {
            await ShowMaterialAlertAsync("提示", "无法获取科目列表，请确保 ClassIsland 正在运行且已启用联动。");
            return;
        }
        
        var existing = Settings.ClassIslandSubjects.Select(s => s.Name).ToHashSet();
        foreach (var subject in subjects)
        {
            if (!existing.Contains(subject))
                Settings.ClassIslandSubjects.Add(new SubjectAction(subject));
        }
        
        await ShowMaterialAlertAsync("提示", $"已刷新，共 {Settings.ClassIslandSubjects.Count} 个科目。");
    }

    private async void ButtonImportClassIslandSubjects_OnClick(object sender, RoutedEventArgs e)
    {
        var subjects = await ClassIslandIpcService.GetSubjectsAsync();
        if (subjects.Count == 0)
        {
            await ShowMaterialAlertAsync("提示", "无法获取科目列表，请确保 ClassIsland 正在运行且已启用联动。");
            return;
        }
        
        var existing = Settings.Subjects.ToHashSet();
        int importedCount = 0;
        foreach (var subject in subjects)
        {
            if (!existing.Contains(subject))
            {
                Settings.Subjects.Add(subject);
                importedCount++;
            }
        }
        
        await ShowMaterialAlertAsync("提示", importedCount > 0
            ? $"成功导入 {importedCount} 个科目至 StickyHomeworks。"
            : "没有新科目需要导入。");
    }

    private async void ButtonTestIpcEvent_OnClick(object sender, RoutedEventArgs e)
    {
        await ShowMaterialAlertAsync(
            "IPC 状态",
            $"连接状态: {ClassIslandIpcService.ConnectionStatus}\n" +
            $"当前时间状态: {ClassIslandIpcService.CurrentState}\n" +
            $"当前科目: {ClassIslandIpcService.CurrentSubjectName}\n" +
            $"上一科目: {ClassIslandIpcService.PreviousSubjectName}\n" +
            $"是否连接: {ClassIslandIpcService.IsConnected}");
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

    /// <summary>与 Material Design 设置页一致的提示对话框（非系统 MessageBox）。</summary>
    private async Task ShowMaterialAlertAsync(string title, string message)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(24),
            MinWidth = 280,
            MaxWidth = 440
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        if (TryFindResource("MaterialDesignHeadline6TextBlock") is Style titleStyle)
            titleBlock.Style = titleStyle;

        var bodyBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 22
        };
        if (TryFindResource("MaterialDesignBody1TextBlock") is Style bodyStyle)
            bodyBlock.Style = bodyStyle;

        var ok = new System.Windows.Controls.Button
        {
            Content = "确定",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0),
            Command = DialogHost.CloseDialogCommand
        };
        if (TryFindResource("MaterialDesignFlatButton") is Style flatStyle)
            ok.Style = flatStyle;

        panel.Children.Add(titleBlock);
        panel.Children.Add(bodyBlock);
        panel.Children.Add(ok);

        await DialogHost.Show(panel, "SettingsWindow");
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

    /// <summary>
    /// 从嵌入资源提取 Updater.exe 到临时目录
    /// </summary>
    /// <returns>提取后的 Updater.exe 文件路径</returns>
    private string ExtractUpdater()
    {
        var extractDir = Path.Combine(Path.GetTempPath(), LocalAppData);
        var updaterPath = Path.Combine(extractDir, "Updater.exe");

        try
        {
            // 确保目录存在
            if (!Directory.Exists(extractDir))
            {
                Directory.CreateDirectory(extractDir);
            }

            // 从嵌入资源提取
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("StickyHomeworks2.Assets.Updater.exe");
            
            if (stream == null)
            {
                throw new InvalidOperationException("无法找到嵌入的 Updater.exe 资源。请确保项目已正确配置嵌入资源。");
            }

            using var fileStream = new FileStream(updaterPath, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.CopyTo(fileStream);

            System.Diagnostics.Debug.WriteLine($"Updater.exe 已提取到: {updaterPath}");
            return updaterPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"提取 Updater.exe 失败: {ex.Message}");
            throw new InvalidOperationException($"提取 Updater.exe 失败: {ex.Message}", ex);
        }
    }

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
                var updateInfo = await client.GetFromJsonAsync<UpdateInfo>(UpdateInfoUrl);



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
                    _updateVersion = updateInfo.Version;
                    versionStatusTextBlock.Text = $"有新版本：{updateInfo.Version}";
                    UpdateStatusTextBlock.Text = $"更新可用";
                    UpdateIcon.Kind = PackIconKind.Upload;
                    DownloadUpdatesButton.Tag = updateInfo.Url;
                    DownloadUpdatesButton.Visibility = Visibility.Visible;
                    CheckUpdatesButton.Visibility = Visibility.Visible;
                    DownloadUpdatesButton.IsEnabled = true;
                    CancelDownloadUpdateButton.IsEnabled = true;
                    CheckUpdatesButton.IsEnabled = true;
                    //LoadTabs(); 
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

        var currentExePath = Process.GetCurrentProcess().MainModule.FileName;
        var appDirectory = Path.GetDirectoryName(currentExePath);
        var currentPid = Environment.ProcessId;

        try
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatusTextBlock.Text = "准备重启...";
                versionStatusTextBlock.Text = "正在准备更新程序...";
                DownloadProgress.IsIndeterminate = true;
            });

            // 提取嵌入的 Updater.exe
            var updaterPath = ExtractUpdater();

            Dispatcher.Invoke(() =>
            {
                UpdateStatusTextBlock.Text = "准备重启...";
                versionStatusTextBlock.Text = "即将重启并应用更新...";
            });

            // 构建启动参数
            var startInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = $"--zip \"{zipFilePath}\" --version \"{_updateVersion}\" --app-dir \"{appDirectory}\" --parent-pid {currentPid}",
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                ErrorDialog = false
            };

            Process.Start(startInfo);

            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            var extractDir = Path.Combine(Path.GetTempPath(), LocalAppData);
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
        versionStatusTextBlock.Text = "已取消更新";
        UpdateStatusTextBlock.Text = $"更新已取消";
        await Task.Delay(150);
        CheckUpdatesButton.IsEnabled = true;
        CheckUpdatesButton.Visibility = Visibility.Visible;
        DownloadUpdatesButton.Visibility = Visibility.Collapsed;
        CancelDownloadUpdateButton.Visibility = Visibility.Collapsed;
        DownloadProgress.Visibility = Visibility.Collapsed;
    }
    private async void DownloadUpdatesButton_Onclick(object sender, RoutedEventArgs e)
    {
        await DownloadUpdatesAsync(sender);
    }
   
    private async void CheckUpdatesButton_Onclick(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync();
        await LoadChangeLogAsync();
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

    //private async void LoadTabs()
    //{
    //    string[] urls = new string[]
    //    {
    //            "https://eb48d3a3.xy.proaa.top/"
    //    };

    //    foreach (var url in urls)
    //    {
    //        string markdownContent = await GetMarkdownContentFromUrl(url);
    //        if (!string.IsNullOrEmpty(markdownContent))
    //        {
    //            string htmlContent = Markdown.ToHtml(markdownContent);
    //            AddTabItem(url, htmlContent);
    //        }
    //    }
    //}

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

    //private void AddTabItem(string url, string content)
    //{
    //    string tabName = System.IO.Path.GetFileNameWithoutExtension(new Uri(url).LocalPath);
    //    TabItem tabItem = new TabItem
    //    {
    //        Header = tabName,
    //        Content = new System.Windows.Controls.WebBrowser { Source = new Uri($"data:text/html,{Uri.EscapeDataString(content)}") }
    //    };
    //    tabControl.Items.Add(tabItem);
    //}


    private async Task LoadChangeLogAsync()
    {
        try
        {
            EnsureHttpClient();
            string markdown = await _httpClient!.GetStringAsync(ChangelogUrl);

            // 使用R0enderToFlowDocument 渲染
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var flowDocument = RenderMarkdownToFlowDocument(markdown);
                flowDocument.Foreground = this.FindResource("MaterialDesignBody") as System.Windows.Media.Brush;
                flowDocument.Background = this.FindResource("MaterialDesignPaper") as System.Windows.Media.Brush;
                foreach (var block in flowDocument.Blocks)
                {
                    if (block is Paragraph paragraph)
                    {
                        paragraph.Foreground = flowDocument.Foreground;
                        paragraph.Background = flowDocument.Background;
                    }
                }

                DocumentViewer.Document = flowDocument;
            });
        }
        catch (HttpRequestException ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DocumentViewer.Document = CreateErrorDocument($"网络连接失败：{ex.Message}");
            });
        }
        catch (Exception ex)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DocumentViewer.Document = CreateErrorDocument($"加载失败：{ex.Message}");
            });
        }
    }

    private FlowDocument RenderMarkdownToFlowDocument(string markdown)
    {
        // 配置管道
        var pipeline = new MarkdownPipelineBuilder()
            .UseSupportedExtensions()
            .Build();

        // 将Markdown转换为 FlowDocument
        var flowDocument = Markdig.Wpf.Markdown.ToFlowDocument(markdown, pipeline);

        // 应用样式
        ApplyMaterialDesignStyles(flowDocument);

        return flowDocument;
    }

    private void ApplyMaterialDesignStyles(FlowDocument document)
    {
        // 设置文档整体式
        document.FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei, Segoe UI");
        document.FontSize = 14;
        document.Foreground = (System.Windows.Media.Brush)FindResource("MaterialDesignBody");
        document.Background = System.Windows.Media.Brushes.Transparent;

    
        document.PagePadding = new Thickness(12);


        var primaryBrush = (System.Windows.Media.Brush)FindResource("PrimaryHueMidBrush");
        var secondaryBrush = (System.Windows.Media.Brush)FindResource("SecondaryHueMidBrush");
        var bodyBrush = (System.Windows.Media.Brush)FindResource("MaterialDesignBody");


        foreach (var block in document.Blocks)
        {
            if (block is Paragraph paragraph)
            {

                if (paragraph.Inlines.FirstOrDefault() is Run run)
                {

                    if (run.FontFamily?.Source.Contains("Consolas") == true)
                    {
                        paragraph.Background = (System.Windows.Media.Brush)FindResource("MaterialDesignPaper");
                        paragraph.BorderBrush = bodyBrush;
                        paragraph.BorderThickness = new Thickness(1);
                        paragraph.Padding = new Thickness(8);
                        paragraph.Margin = new Thickness(0, 8, 0, 8);
                    }
                }

                paragraph.Margin = new Thickness(0, 4, 0, 4);
            }
            else if (block is List list)
            {
                list.Margin = new Thickness(24, 8, 0, 8);
            }
        }
    }

    private FlowDocument CreateErrorDocument(string errorMessage)
    {
        var flowDocument = new FlowDocument
        {
            FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei, Segoe UI"),
            FontSize = 14,
            Foreground = (System.Windows.Media.Brush)FindResource("MaterialDesignBody")
        };

        var title = new Paragraph(new Run("无法加载更新日志"))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = (System.Windows.Media.Brush)FindResource("MaterialDesignValidationErrorBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var message = new Paragraph(new Run(errorMessage))
        {
            Margin = new Thickness(0, 0, 0, 8)
        };


        flowDocument.Blocks.Add(title);
        flowDocument.Blocks.Add(message);

        return flowDocument;
    }

   

    //<<<更新逻辑:结束>>>


    private void MultipleOpeningsText_OnClick(object sender, RoutedEventArgs e)
    {
        SingleInstanceWarning warningWindow = new();
        warningWindow.ShowDialog();
    }
    private void ButtonGithub_Click(object sender, RoutedEventArgs e)
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
    private void ButtonDocs_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://sh2.xn--fjqu59cvx0aoqi.icu/doc/guide/",
            UseShellExecute = true
        });
    }

    private void SettingsExpanderCard_Loaded()
    {

    }

    #region 作业模板

    private void PersistHomeworkTemplate()
    {
        HomeworkTemplateConfig.Normalize(Settings.HomeworkTemplate);
        Settings.HomeworkTemplate.PruneSubjectBooksNotInSubjects(Settings.Subjects);
        _settingsService.SaveSettings();
    }

    private void RequestPersistHomeworkTemplate()
    {
        if (_homeworkTemplatePersistDebounceTimer == null)
        {
            _homeworkTemplatePersistDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _homeworkTemplatePersistDebounceTimer.Tick += (_, _) =>
            {
                _homeworkTemplatePersistDebounceTimer?.Stop();
                PersistHomeworkTemplate();
            };
        }
        else
            _homeworkTemplatePersistDebounceTimer.Stop();

        _homeworkTemplatePersistDebounceTimer.Start();
    }

    private void RefreshHomeworkTemplateCommonBookKeys()
    {
        _homeworkTemplateCommonBookKeys.Clear();
        foreach (var k in Settings.HomeworkTemplate.CommonBooks.Keys.OrderBy(static x => x))
            _homeworkTemplateCommonBookKeys.Add(k);
    }

    private void RefreshHomeworkTemplateSubjectBookKeys()
    {
        _homeworkTemplateSubjectBookKeys.Clear();
        var subject = ViewModel.HomeworkTemplateSelectedSubject;
        if (string.IsNullOrEmpty(subject))
            return;
        if (!Settings.HomeworkTemplate.SubjectBooks.TryGetValue(subject, out var inner))
            return;
        foreach (var k in inner.Keys.OrderBy(static x => x))
            _homeworkTemplateSubjectBookKeys.Add(k);
    }

    private Dictionary<string, ObservableCollection<string>> GetOrCreateSubjectBooksInner(string subject)
    {
        if (!Settings.HomeworkTemplate.SubjectBooks.TryGetValue(subject, out var inner))
        {
            inner = new Dictionary<string, ObservableCollection<string>>();
            Settings.HomeworkTemplate.SubjectBooks[subject] = inner;
        }

        return inner;
    }

    private void TabHomeworkTemplate_OnLoaded(object sender, RoutedEventArgs e)
    {
        HomeworkTemplateConfig.Normalize(Settings.HomeworkTemplate);
        Settings.HomeworkTemplate.PruneSubjectBooksNotInSubjects(Settings.Subjects);
        RefreshHomeworkTemplateCommonBookKeys();
        if (ViewModel.HomeworkTemplateSelectedSubject == null ||
            !Settings.Subjects.Contains(ViewModel.HomeworkTemplateSelectedSubject))
            ViewModel.HomeworkTemplateSelectedSubject = Settings.Subjects.FirstOrDefault();
        RefreshHomeworkTemplateSubjectBookKeys();
        LbHomeworkTemplateCommonParts.ItemsSource = null;
        LbHomeworkTemplateSubjectParts.ItemsSource = null;
        PersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateAddQuickAction_OnClick(object sender, RoutedEventArgs e)
    {
        var text = TbHomeworkTemplateNewQuickAction.Text.Trim();
        if (string.IsNullOrEmpty(text))
            return;
        Settings.HomeworkTemplate.QuickActions.Add(text);
        TbHomeworkTemplateNewQuickAction.Clear();
        RequestPersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateRemoveQuickAction_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not string text)
            return;
        Settings.HomeworkTemplate.QuickActions.Remove(text);
        RequestPersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateAddCommonBook_OnClick(object sender, RoutedEventArgs e)
    {
        var name = TbHomeworkTemplateNewCommonBook.Text.Trim();
        if (string.IsNullOrEmpty(name))
            return;
        if (Settings.HomeworkTemplate.CommonBooks.ContainsKey(name))
            return;
        Settings.HomeworkTemplate.CommonBooks[name] = new ObservableCollection<string>();
        TbHomeworkTemplateNewCommonBook.Clear();
        RefreshHomeworkTemplateCommonBookKeys();
        RequestPersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateDeleteCommonBook_OnClick(object sender, RoutedEventArgs e)
    {
        if (LbHomeworkTemplateCommonBooks.SelectedItem is not string key)
            return;
        Settings.HomeworkTemplate.CommonBooks.Remove(key);
        RefreshHomeworkTemplateCommonBookKeys();
        LbHomeworkTemplateCommonParts.ItemsSource = null;
        RequestPersistHomeworkTemplate();
    }

    private void LbHomeworkTemplateCommonBooks_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LbHomeworkTemplateCommonBooks.SelectedItem is not string key)
        {
            LbHomeworkTemplateCommonParts.ItemsSource = null;
            return;
        }

        if (!Settings.HomeworkTemplate.CommonBooks.TryGetValue(key, out var parts))
        {
            parts = new ObservableCollection<string>();
            Settings.HomeworkTemplate.CommonBooks[key] = parts;
        }

        LbHomeworkTemplateCommonParts.ItemsSource = parts;
    }

    private void ButtonHomeworkTemplateAddCommonPart_OnClick(object sender, RoutedEventArgs e)
    {
        var text = TbHomeworkTemplateNewCommonPart.Text.Trim();
        if (string.IsNullOrEmpty(text))
            return;
        if (LbHomeworkTemplateCommonBooks.SelectedItem is not string key)
            return;
        if (!Settings.HomeworkTemplate.CommonBooks.TryGetValue(key, out var parts))
        {
            parts = new ObservableCollection<string>();
            Settings.HomeworkTemplate.CommonBooks[key] = parts;
        }

        parts.Add(text);
        TbHomeworkTemplateNewCommonPart.Clear();
        RequestPersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateRemoveCommonPart_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not string part)
            return;
        if (LbHomeworkTemplateCommonParts.ItemsSource is not ObservableCollection<string> oc)
            return;
        oc.Remove(part);
        RequestPersistHomeworkTemplate();
    }

    private void CbHomeworkTemplateSubject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshHomeworkTemplateSubjectBookKeys();
        LbHomeworkTemplateSubjectBooks.SelectedIndex = -1;
        LbHomeworkTemplateSubjectParts.ItemsSource = null;
    }

    private void ButtonHomeworkTemplateAddSubjectBook_OnClick(object sender, RoutedEventArgs e)
    {
        var subject = ViewModel.HomeworkTemplateSelectedSubject;
        if (string.IsNullOrEmpty(subject))
            return;
        var name = TbHomeworkTemplateNewSubjectBook.Text.Trim();
        if (string.IsNullOrEmpty(name))
            return;
        var inner = GetOrCreateSubjectBooksInner(subject);
        if (!inner.ContainsKey(name))
            inner[name] = new ObservableCollection<string>();
        TbHomeworkTemplateNewSubjectBook.Clear();
        RefreshHomeworkTemplateSubjectBookKeys();
        RequestPersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateDeleteSubjectBook_OnClick(object sender, RoutedEventArgs e)
    {
        var subject = ViewModel.HomeworkTemplateSelectedSubject;
        if (string.IsNullOrEmpty(subject))
            return;
        if (LbHomeworkTemplateSubjectBooks.SelectedItem is not string bookKey)
            return;
        if (!Settings.HomeworkTemplate.SubjectBooks.TryGetValue(subject, out var inner))
            return;
        inner.Remove(bookKey);
        RefreshHomeworkTemplateSubjectBookKeys();
        LbHomeworkTemplateSubjectParts.ItemsSource = null;
        RequestPersistHomeworkTemplate();
    }

    private void LbHomeworkTemplateSubjectBooks_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var subject = ViewModel.HomeworkTemplateSelectedSubject;
        if (string.IsNullOrEmpty(subject))
        {
            LbHomeworkTemplateSubjectParts.ItemsSource = null;
            return;
        }

        if (LbHomeworkTemplateSubjectBooks.SelectedItem is not string bookKey)
        {
            LbHomeworkTemplateSubjectParts.ItemsSource = null;
            return;
        }

        var inner = GetOrCreateSubjectBooksInner(subject);
        if (!inner.TryGetValue(bookKey, out var parts))
        {
            parts = new ObservableCollection<string>();
            inner[bookKey] = parts;
        }

        LbHomeworkTemplateSubjectParts.ItemsSource = parts;
    }

    private void ButtonHomeworkTemplateAddSubjectPart_OnClick(object sender, RoutedEventArgs e)
    {
        var subject = ViewModel.HomeworkTemplateSelectedSubject;
        if (string.IsNullOrEmpty(subject))
            return;
        var text = TbHomeworkTemplateNewSubjectPart.Text.Trim();
        if (string.IsNullOrEmpty(text))
            return;
        if (LbHomeworkTemplateSubjectBooks.SelectedItem is not string bookKey)
            return;
        var inner = GetOrCreateSubjectBooksInner(subject);
        if (!inner.TryGetValue(bookKey, out var parts))
        {
            parts = new ObservableCollection<string>();
            inner[bookKey] = parts;
        }

        parts.Add(text);
        TbHomeworkTemplateNewSubjectPart.Clear();
        RequestPersistHomeworkTemplate();
    }

    private void ButtonHomeworkTemplateRemoveSubjectPart_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not string part)
            return;
        if (LbHomeworkTemplateSubjectParts.ItemsSource is not ObservableCollection<string> oc)
            return;
        oc.Remove(part);
        RequestPersistHomeworkTemplate();
    }

    #endregion
}
