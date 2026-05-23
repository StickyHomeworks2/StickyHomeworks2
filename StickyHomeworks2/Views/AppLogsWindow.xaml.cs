using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ElysiaFramework;
using ElysiaFramework.Interfaces;
using StickyHomeworks.Models.Logging;
using StickyHomeworks.Services.Logging;
using Microsoft.Extensions.Logging;

namespace StickyHomeworks.Views;

public partial class AppLogsWindow : ElysiaFramework.Controls.MyWindow, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public AppLogService AppLogService { get; }

    private bool _isOpened;
    private bool _autoScroll = true;

    private string _filterText = "";
    private bool _isFilteredCritical = true;
    private bool _isFilteredError = true;
    private bool _isFilteredWarning = true;
    private bool _isFilteredInfo = true;
    private bool _isFilteredDebug;
    private bool _isFilteredTrace;

    public string FilterText { get => _filterText; set { if (_filterText == value) return; _filterText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterText))); } }
    public bool IsFilteredCritical { get => _isFilteredCritical; set { if (_isFilteredCritical == value) return; _isFilteredCritical = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilteredCritical))); } }
    public bool IsFilteredError { get => _isFilteredError; set { if (_isFilteredError == value) return; _isFilteredError = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilteredError))); } }
    public bool IsFilteredWarning { get => _isFilteredWarning; set { if (_isFilteredWarning == value) return; _isFilteredWarning = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilteredWarning))); } }
    public bool IsFilteredInfo { get => _isFilteredInfo; set { if (_isFilteredInfo == value) return; _isFilteredInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilteredInfo))); } }
    public bool IsFilteredDebug { get => _isFilteredDebug; set { if (_isFilteredDebug == value) return; _isFilteredDebug = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilteredDebug))); } }
    public bool IsFilteredTrace { get => _isFilteredTrace; set { if (_isFilteredTrace == value) return; _isFilteredTrace = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilteredTrace))); } }

    private IThemeService? ThemeService { get; }

    public AppLogsWindow(AppLogService appLogService)
    {
        AppLogService = appLogService;
        InitializeComponent();
        DataContext = this;
        MainListView.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnListViewScrollChanged));
        try
        {
            ThemeService = AppEx.GetService<IThemeService>();
            ThemeService.ThemeUpdated += OnThemeUpdated;
        }
        catch { }
        UpdateLogBrushes();
    }

    private void OnListViewScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange == 0)
            _autoScroll = Math.Abs(e.VerticalOffset + e.ViewportHeight - e.ExtentHeight) < 1;
        if (_autoScroll && e.ExtentHeightChange > 0 && AppLogService.Logs.Count > 0)
            MainListView.ScrollIntoView(AppLogService.Logs[^1]);
    }

    private void OnThemeUpdated(object? sender, ThemeUpdatedEventArgs e) => UpdateLogBrushes();

    private void UpdateLogBrushes()
    {
        var isDark = ThemeService?.CurrentRealThemeMode == 1;
        Resources["LogWarningBrush"] = new SolidColorBrush(isDark ? Colors.Orange : Color.FromRgb(0xFF, 0xA5, 0x00));
        Resources["LogErrorBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0xFF, 0x6B, 0x6B) : Color.FromRgb(0xCD, 0x5C, 0x5C));
        Resources["LogCriticalBrush"] = new SolidColorBrush(isDark ? Color.FromRgb(0xFF, 0x44, 0x44) : Colors.Red);
        Resources["LogWarningBackgroundBrush"] = new SolidColorBrush(isDark ? Color.FromArgb(0x33, 0xFF, 0xA5, 0x00) : Color.FromArgb(0x22, 0xFF, 0xA5, 0x00));
        Resources["LogErrorBackgroundBrush"] = new SolidColorBrush(isDark ? Color.FromArgb(0x33, 0xFF, 0x6B, 0x6B) : Color.FromArgb(0x22, 0xCD, 0x5C, 0x5C));
        Resources["LogCriticalBackgroundBrush"] = new SolidColorBrush(isDark ? Color.FromArgb(0x33, 0xFF, 0x44, 0x44) : Color.FromArgb(0x22, 0xFF, 0x00, 0x00));
    }

    public void Open()
    {
        if (!_isOpened) { _isOpened = true; Show(); }
        else { if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal; Activate(); }
        AppEx.GetService<ILogger<AppLogsWindow>>().LogInformation("日志窗口已打开");
    }

    private void AppLogsWindow_OnClosing(object? sender, CancelEventArgs e) { e.Cancel = true; Hide(); _isOpened = false; }

    private void LogsSource_OnFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not LogEntry i) return;
        bool levelMatch = (IsFilteredCritical && i.LogLevel == LogLevel.Critical) ||
                          (IsFilteredError && i.LogLevel == LogLevel.Error) ||
                          (IsFilteredWarning && i.LogLevel == LogLevel.Warning) ||
                          (IsFilteredInfo && i.LogLevel == LogLevel.Information) ||
                          (IsFilteredDebug && i.LogLevel == LogLevel.Debug) ||
                          (IsFilteredTrace && i.LogLevel == LogLevel.Trace);
        e.Accepted = levelMatch && (string.IsNullOrWhiteSpace(FilterText) ||
                     i.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                     i.CategoryName.Contains(FilterText));
    }

    private void RefreshView()
    {
        if (FindResource("LogsSource") is CollectionViewSource a) a.View?.Refresh();
        if (AppLogService.Logs.Count > 0) MainListView.ScrollIntoView(AppLogService.Logs[^1]);
    }

    private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e) => RefreshView();
    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshView();
    private void MainListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void ButtonCopySelectedLogs_OnClick(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetDataObject(string.Join('\n', MainListView.SelectedItems.OfType<object>().Select(i => i.ToString()!))); }
        catch (Exception ex) { AppEx.GetService<ILogger<AppLogsWindow>>().LogError(ex, "无法复制日志到剪切板"); }
    }

    private void ButtonClearLogs_OnClick(object sender, RoutedEventArgs e) => AppLogService.Logs.Clear();
}
