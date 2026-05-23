using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using ClassIsland.Services;
using ElysiaFramework;
using ElysiaFramework.Interfaces;
using ElysiaFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Core.Context;
using StickyHomeworks.Models.Logging;
using StickyHomeworks.Services;
using StickyHomeworks.Services.Logging;
using StickyHomeworks.Views;
using StickyHomeworks2.Helpers;
using StickyHomeworks.Behaviors;
using MessageBox = System.Windows.MessageBox;

namespace StickyHomeworks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : AppEx
{
    private static Mutex? Mutex;

    private static SingleInstanceWarning warningWindow;

    public static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    private ILogger<App>? Logger { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        //AppContext.SetSwitch(@"Switch.System.Windows.Controls.DoNotAugmentWordBreakingUsingSpeller", true);
        Mutex = new Mutex(true, "StickyHomeworks.Lock", out var createNew);
        if (!createNew)
        {
            warningWindow = new SingleInstanceWarning();
            warningWindow.ShowDialog();
            Environment.Exit(0);
        }

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddDbContext<AppDbContext>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<ProfileService>();
                services.AddSingleton<SettingsWindow>();
                services.AddSingleton<WallpaperPickingService>();
                services.AddHostedService<ThemeBackgroundService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<HomeworkEditWindow>();
                services.AddSingleton<CrashWindow>();
                services.AddSingleton<WindowFocusObserverService>();
                services.AddSingleton<TimeMachineService>();
                services.AddSingleton<ImageService>();
                services.AddSingleton<ClassIslandIpcService>();
                services.AddHostedService(sp => sp.GetRequiredService<ClassIslandIpcService>());
                services.AddSingleton<AppLogService>();
                services.AddSingleton<ILoggerProvider, AppLoggerProvider>();
                services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
                services.AddSingleton<AppLogsWindow>();
                services.AddLogging(builder =>
                {
                    LogMaskingHelper.Rules.Add(new LogMaskRule(new(@"(latitude=)(\d*\.?\d*)"), 2));
                    LogMaskingHelper.Rules.Add(new LogMaskRule(new(@"(longitude=)(\d*\.?\d*)"), 2));
#if DEBUG
                    builder.SetMinimumLevel(LogLevel.Debug);
#endif
                });
                services.AddSingleton<ViewModels.TimeMachineViewModel>();
                services.AddSingleton<TimeMachineWindow>();
            }).
            Build();
        _ = Host.StartAsync();
        GetService<AppDbContext>();
        MainWindow = GetService<MainWindow>();
        GetService<MainWindow>().Show();
        LinkConfirmationHelper.SetLogger(GetService<ILoggerFactory>().CreateLogger(typeof(LinkConfirmationHelper).FullName));
        RichTextBoxBindingBehavior.SetLogger(GetService<ILogger<RichTextBoxBindingBehavior>>());

        Logger = GetService<ILogger<App>>();
        Logger.LogInformation("StickyHomeworks2 v{Version} 正在启动", AppVersion);

        var lifetime = GetService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() => Logger.LogInformation("应用已启动"));
        lifetime.ApplicationStopping.Register(() => Logger.LogInformation("应用正在停止"));
        lifetime.ApplicationStopped.Register(() => Logger.LogInformation("应用已停止"));

        System.Windows.Diagnostics.BindingDiagnostics.BindingFailed += (o, args) =>
        {
            if (args.EventType == TraceEventType.Verbose)
            {
                Logger.LogTrace("{Message}", args.Message);
            }
            else
            {
                Logger.LogWarning("{Message}", args.Message);
            }
        };

        base.OnStartup(e);
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Logger?.LogCritical(e.Exception, "未处理的异常");
        var cw = GetService<CrashWindow>();
        cw.CrashInfo = e.Exception.ToString();
        cw.Exception = e.Exception;
        cw.OpenWindow();
    }

    public static void ReleaseLock()
    {
        Mutex?.ReleaseMutex();
    }


}