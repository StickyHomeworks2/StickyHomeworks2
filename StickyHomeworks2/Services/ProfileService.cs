using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class ProfileService : IHostedService, INotifyPropertyChanged
{
    private Profile _profile = new();
    private readonly SettingsService _settingsService;
    private readonly ILogger<ProfileService> _logger;
    private PropertyChangedEventHandler? _profileHandler;

    public event EventHandler? ProfileSaved;

    public ProfileService(IHostApplicationLifetime applicationLifetime, SettingsService settingsService, ILogger<ProfileService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        LoadProfile();
        SubscribeProfile();
    }

    private void SubscribeProfile()
    {
        if (_profileHandler != null)
            Profile.PropertyChanged -= _profileHandler;
        _profileHandler = (sender, args) => SaveProfile();
        Profile.PropertyChanged += _profileHandler;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void LoadProfile()
    {
        if (!File.Exists("./Profile.json"))
        {
            _logger.LogInformation("未找到 Profile.json，使用默认配置: {Path}", Path.GetFullPath("./Profile.json"));
            return;
        }
        var json = File.ReadAllText("./Profile.json");
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            _logger.LogInformation("已加载 Profile.json: {Path} 共 {HomeworkCount} 条作业", Path.GetFullPath("./Profile.json"), r.Homeworks?.Count ?? 0);
            Profile = r;
        }
        else
        {
            _logger.LogWarning("Profile.json 反序列化失败，使用默认配置");
        }
    }

    public List<Homework> CleanupOutdated()
    {
        var useDelayed = _settingsService.Settings.DelayedCleanupEnabled && _settingsService.Settings.Autooutwork;
        
        var rm = Profile.Homeworks.Where(i => 
            i.DueTime.Date < DateTime.Today.Date && 
            (!useDelayed || (i.FirstExpiredShowTime.HasValue && i.FirstExpiredShowTime.Value.Date < DateTime.Today.Date))).ToList();
        
        foreach (var i in rm) Profile.Homeworks.Remove(i);
        if (rm.Count > 0)
        {
            var details = string.Join(", ", rm.Select(h => $"{h.Subject ?? "(无科目)"}(截止:{h.DueTime:yyyy-MM-dd})"));
            _logger.LogInformation("清理了 {Count} 条过期作业: {Details}", rm.Count, details);
        }
        return rm;
    }

    public List<Homework> GetExpiredHomeworks() => Profile.Homeworks.Where(i => i.DueTime.Date < DateTime.Today.Date).ToList();

    public void SaveProfile()
    {
        var path = Path.GetFullPath("./Profile.json");
        File.WriteAllText(path, JsonSerializer.Serialize<Profile>(Profile));
        _logger.LogInformation("写入 Profile.json: {Path} 作业数: {Count}", path, Profile.Homeworks.Count);
        ProfileSaved?.Invoke(this, EventArgs.Empty);
    }

    public Profile Profile
    {
        get => _profile;
        set
        {
            if (Equals(value, _profile)) return;
            _profile = value;
            OnPropertyChanged();
            SubscribeProfile();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}