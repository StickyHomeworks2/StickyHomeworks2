using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace StickyHomeworks.Services.Logging;

public class FileLogger(FileLoggerProvider provider, string categoryName) : ILogger
{
    private static readonly AsyncLocal<Stack<object>> ScopeStack = new AsyncLocal<Stack<object>>();
    private FileLoggerProvider Provider { get; } = provider;
    private string CategoryName { get; } = categoryName;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        List<string> scopes = [];
        if (ScopeStack.Value != null)
        {
            scopes.AddRange(ScopeStack.Value.Select(scope => (scope.ToString() ?? "") + "=>"));
        }
        var message = string.Join("", scopes) + formatter(state, exception) + (exception != null ? "\n" + exception : "");
        message = Models.Logging.LogMaskingHelper.MaskLog(message);
        Provider.WriteLog($"{DateTime.Now}|{logLevel}|{CategoryName}|{message}");
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        ScopeStack.Value ??= new Stack<object>();
        ScopeStack.Value.Push(state);

        return Models.Logging.LoggingScope.Create(() => ScopeStack.Value.Pop());
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly Stream? _logStream;
    private readonly StreamWriter? _logWriter;

    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    private const int LogRetentionDays = 30;

    private readonly object _lock = new object();

    public static string AppLogFolderPath => Path.Combine(
        AppContext.BaseDirectory, "Logs");

    public static string GetLogFileName()
    {
        var n = 1;
        var logs = GetLogs();
        string filename;
        do
        {
            filename = $"log-{DateTime.Now:yyyy-M-d-HH-mm-ss}-{n}.log";
            n++;
        } while (logs.Contains(filename));

        return filename;
    }

    private bool _canWrite = true;

    public FileLoggerProvider()
    {
        try
        {
            Directory.CreateDirectory(AppLogFolderPath);
            var logs = Directory.GetFiles(AppLogFolderPath);
            var currentLogFile = GetLogFileName();
            _logStream = File.Open(Path.Combine(AppLogFolderPath, currentLogFile), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _logWriter = new StreamWriter(_logStream)
            {
                AutoFlush = true
            };
            _ = Task.Run(() => ProcessPreviousLogs(logs, currentLogFile));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void ProcessPreviousLogs(string[] logs, string currentLogFile)
    {
        foreach (var i in logs.Where(x => Path.GetFileName(x) != currentLogFile && Path.GetExtension(x) == ".log"))
        {
            try
            {
                CompressFileAndDelete(i);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        var now = DateTime.Now;
        foreach (var i in logs.Where(x => (now - File.GetLastWriteTime(x)).TotalDays > LogRetentionDays &&
                                          Path.GetFileName(x) != currentLogFile &&
                                          (x.EndsWith(".log") || x.EndsWith(".log.gz"))))
        {
            try
            {
                File.Delete(i);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private static void CompressFileAndDelete(string path)
    {
        using var originalFileStream = File.Open(path, FileMode.Open);
        using var compressedFileStream = File.Create(path + ".gz");
        using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
        originalFileStream.CopyTo(compressor);
        compressor.Close();
        originalFileStream.Close();
        File.Delete(path);
    }

    private static List<string?> GetLogs()
    {
        if (!Directory.Exists(AppLogFolderPath))
            return [];
        return Directory.GetFiles(AppLogFolderPath).Select(Path.GetFileName).ToList();
    }

    internal void WriteLog(string log)
    {
        lock (_lock)
        {
            try
            {
                if (!_canWrite)
                {
                    return;
                }
                _logWriter?.WriteLine(log);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _canWrite = false;
            }
        }
    }

    public void Dispose()
    {
        _logWriter?.Close();
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new FileLogger(this, categoryName));
    }
}
