using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models.Logging;

namespace StickyHomeworks.Services.Logging;

public class AppLogService
{
    public static readonly int MaxLogEntries = 1000;

    public ObservableCollection<LogEntry> Logs { get; } = new();

    public void AddLog(LogEntry log)
    {
        var dispatcher = Application.Current?.Dispatcher;
        _ = dispatcher?.InvokeAsync(() =>
        {
            Logs.Add(log);
            while (Logs.Count > MaxLogEntries)
            {
                Logs.RemoveAt(0);
            }
        });
    }
}

public class AppLogger(AppLogService appLogService, string categoryName) : ILogger
{
    private AppLogService AppLogService { get; } = appLogService;

    private string CategoryName { get; } = categoryName;

    private static readonly AsyncLocal<Stack<object>> ScopeStack = new AsyncLocal<Stack<object>>();

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        List<string> scopes = [];
        if (ScopeStack.Value != null)
        {
            scopes.AddRange(ScopeStack.Value.Select(scope => (scope.ToString() ?? "") + " => "));
        }
        var message = string.Join("", scopes) + formatter(state, exception) + (exception != null ? "\n" + exception : "");
        AppLogService.AddLog(new LogEntry()
        {
            LogLevel = logLevel,
            Message = LogMaskingHelper.MaskLog(message),
            CategoryName = CategoryName,
            Exception = exception
        });
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        ScopeStack.Value ??= new Stack<object>();
        ScopeStack.Value.Push(state);

        return LoggingScope.Create(() => ScopeStack.Value.Pop());
    }
}

public class AppLoggerProvider(AppLogService appLogService) : ILoggerProvider
{
    private AppLogService AppLogService { get; } = appLogService;

    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new AppLogger(AppLogService, categoryName);
    }
}
