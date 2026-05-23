using System.ComponentModel;
using System.Diagnostics;
using ElysiaFramework;
using System.Windows.Media;
using ClassIsland.Services;
using ElysiaFramework.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace StickyHomeworks.Services;

public class ThemeBackgroundService : IHostedService
{
    private readonly ILogger<ThemeBackgroundService> _logger;

    private SettingsService SettingsService { get; }

    private IThemeService ThemeService { get; }

    private WallpaperPickingService WallpaperPickingService { get; }

    public ThemeBackgroundService(SettingsService settingsService, IThemeService themeService, WallpaperPickingService wallpaperPickingService, ILogger<ThemeBackgroundService> logger)
    {
        SettingsService = settingsService;
        ThemeService = themeService;
        WallpaperPickingService = wallpaperPickingService;
        _logger = logger;
        SettingsService.OnSettingsChanged += SettingsServiceOnOnSettingsChanged;
        SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
        WallpaperPickingService.WallpaperColorPlatteChanged += WallpaperPickingServiceOnWallpaperColorPlatteChanged;
    }

    private void WallpaperPickingServiceOnWallpaperColorPlatteChanged(object? sender, EventArgs e)
    {
        UpdateTheme();
    }

    private Stopwatch UpdateStopWatch { get; } = new();

    private async void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        await WallpaperPickingService.GetWallpaperAsync();
        //UpdateTheme();
    }

    private void SettingsServiceOnOnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!WallpaperPickingService.IsWorking)
        {
            UpdateTheme();
        }
    }

    private void UpdateTheme()
    {
        if (UpdateStopWatch is { IsRunning: true, ElapsedMilliseconds: < 300 })
        {
            return;
        }
        UpdateStopWatch.Restart();
        var primary = Colors.DodgerBlue;
        var secondary = Colors.DodgerBlue;
        switch (SettingsService.Settings.ColorSource)
        {
            case 0: //custom
                primary = SettingsService.Settings.PrimaryColor;
                secondary = SettingsService.Settings.SecondaryColor;
                break;
            case 1:
                primary = secondary = SettingsService.Settings.SelectedPlatte;
                break;
            case 2:
                try
                {
                    NativeWindowHelper.DwmGetColorizationColor(out var color, out _);
                    var c = NativeWindowHelper.GetColor(color);
                    primary = secondary = c;
                }
                catch
                {
                    // ignored
                }

                break;
        }

        ThemeService.SetTheme(SettingsService.Settings.Theme, primary, secondary);
        _logger.LogInformation("设置主题: Theme={Theme} ColorSource={ColorSource}", SettingsService.Settings.Theme, SettingsService.Settings.ColorSource);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        UpdateTheme();
        _ = WallpaperPickingService.GetWallpaperAsync();
        UpdateStopWatch.Start();
        _logger.LogInformation("ThemeBackgroundService 已启动");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged;
        return Task.CompletedTask;
    }
}