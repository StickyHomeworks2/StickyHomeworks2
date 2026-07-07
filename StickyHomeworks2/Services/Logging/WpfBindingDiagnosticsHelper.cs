using System.Diagnostics;
using System.Reflection;
using System.Windows.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace StickyHomeworks.Services.Logging;

public static class WpfBindingDiagnosticsHelper
{
    private const string XamlDiagnosticsSourceInfoEnvVar = "ENABLE_XAML_DIAGNOSTICS_SOURCE_INFO";
    private const string ManagedTracingRegistryPath = @"Software\Microsoft\Tracing\WPF";

    private static WpfBindingTraceListener? _traceListener;

    public static void PrepareEnvironment()
    {
        Environment.SetEnvironmentVariable(XamlDiagnosticsSourceInfoEnvVar, "1");
        EnableManagedTracingRegistry();
        ForceAvTraceRefreshedFlag();
    }

    public static void Initialize(ILogger logger)
    {
        PrepareEnvironment();
        RefreshAndConfigureTraceSource(logger);

        try
        {
            EnableBindingDiagnostics();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "无法通过反射启用 WPF BindingDiagnostics");
        }

        BindingDiagnostics.BindingFailed += (_, args) =>
        {
            logger.LogWarning("{Message}", args.Message);
        };
    }

    private static void RefreshAndConfigureTraceSource(ILogger logger)
    {
        _traceListener ??= new WpfBindingTraceListener(logger);

        PresentationTraceSources.Refresh();

        ApplyTraceSourceConfiguration(_traceListener);
        ForceTraceDataAvTraceEnabled();
    }

    private static void ApplyTraceSourceConfiguration(WpfBindingTraceListener listener)
    {
        var source = PresentationTraceSources.DataBindingSource;
        source.Switch.Level = SourceLevels.Warning;

        if (!source.Listeners.Contains(listener))
        {
            source.Listeners.Add(listener);
        }
    }

    private static void EnableManagedTracingRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(ManagedTracingRegistryPath);
            key?.SetValue("ManagedTracing", 1, RegistryValueKind.DWord);
        }
        catch
        {
            // 无权限写注册表时仍可通过 Refresh 启用 Trace
        }
    }

    private static void ForceAvTraceRefreshedFlag()
    {
        var avTraceType = typeof(PresentationTraceSources).Assembly.GetType("MS.Internal.AvTrace");
        avTraceType?.GetField("_hasBeenRefreshed", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, true);
    }

    private static void ForceTraceDataAvTraceEnabled()
    {
        var traceDataType = typeof(System.Windows.Data.Binding).Assembly.GetType("MS.Internal.TraceData");
        var avTrace = traceDataType?.GetField("_avTrace", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
        if (avTrace == null)
        {
            return;
        }

        avTrace.GetType().GetMethod("Refresh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.Invoke(avTrace, null);
        avTrace.GetType().GetField("_isEnabled", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(avTrace, true);
    }

    private static void EnableBindingDiagnostics()
    {
        typeof(BindingDiagnostics).GetProperty(
                "IsEnabled",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(null, true);
    }

    private sealed class WpfBindingTraceListener(ILogger logger) : TraceListener
    {
        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            logger.LogWarning("{Message}", message.Trim());
        }
    }
}
