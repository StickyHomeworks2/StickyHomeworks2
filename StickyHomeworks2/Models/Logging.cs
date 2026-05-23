using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace StickyHomeworks.Models.Logging;

public partial class LogEntry : ObservableRecipient
{
    public DateTime Time { get; set; } = DateTime.Now;
    public LogLevel LogLevel { get; set; } = LogLevel.None;
    public string Message { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public Exception? Exception { get; set; }

    public override string ToString() => $"[{Time}] [{LogLevel}] {CategoryName}:\n{Message}";
}

public record LogMaskRule(Regex Regex, int MatchIndex);

public static class LoggingScope
{
    public static IDisposable Create(Action onDispose) => new Scope(onDispose);

    private class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose?.Invoke();
    }
}

public static class LogMaskingHelper
{
    public static List<LogMaskRule> Rules { get; } = [];

    public static string MaskLog(string log, string replace = "***")
    {
        return Rules.Aggregate(log, (current, rule) => rule.Regex.Replace(current, match =>
        {
            if (match.Groups.Count == 1)
                return match.Groups[0].Value;

            List<string> parts = [];
            for (var i = 1; i < match.Groups.Count; i++)
                parts.Add(i != rule.MatchIndex ? match.Groups[i].Value : replace);

            return string.Join("", parts);
        }));
    }
}
