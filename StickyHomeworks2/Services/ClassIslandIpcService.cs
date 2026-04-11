using System.ComponentModel;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using ElysiaFramework;
using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;

public class ClassIslandIpcService : IHostedService, INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private IpcClient? _ipcClient;
    private IPublicLessonsService? _lessonsService;
    private string _currentSubjectName = "";
    private bool _isConnected;
    private string _connectionStatus = "未连接";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ClassStateChanged;

    public string CurrentSubjectName
    {
        get => _currentSubjectName;
        private set
        {
            if (value == _currentSubjectName) return;
            _currentSubjectName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSubjectName)));
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (value == _isConnected) return;
            _isConnected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set
        {
            if (value == _connectionStatus) return;
            _connectionStatus = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ConnectionStatus)));
        }
    }

    public ClassIslandIpcService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settingsService.Settings.IsClassIslandIpcEnabled)
            await ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Disconnect();
    }

    public async Task ConnectAsync()
    {
        try
        {
            Disconnect();
            _ipcClient = new IpcClient();
            _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.OnClassNotifyId, OnClassChanged);
            _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.OnBreakingTimeNotifyId, OnBreakingTime);
            _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.OnAfterSchoolNotifyId, OnAfterSchool);
            _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.CurrentTimeStateChangedNotifyId, OnTimeStateChanged);
            await _ipcClient.Connect();
            _lessonsService = _ipcClient.Provider.CreateIpcProxy<IPublicLessonsService>(_ipcClient.PeerProxy!);
            IsConnected = true;
            ConnectionStatus = "已连接";
            RefreshCurrentSubject();
        }
        catch
        {
            IsConnected = false;
            ConnectionStatus = "连接失败";
        }
    }

    public void Disconnect()
    {
        _ipcClient = null;
        _lessonsService = null;
        IsConnected = false;
        ConnectionStatus = "未连接";
        CurrentSubjectName = "";
    }

    public async Task<List<string>> GetSubjectsAsync()
    {
        if (_ipcClient == null) return [];
        try
        {
            var profileService = _ipcClient.Provider.CreateIpcProxy<IPublicProfileService>(_ipcClient.PeerProxy!);
            var profile = await Task.Run(() => profileService.Profile);
            return profile?.Subjects?.Select(s => s.Value.Name).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void RefreshCurrentSubject()
    {
        if (_lessonsService == null) return;
        try
        {
            CurrentSubjectName = _lessonsService.CurrentSubject?.Name ?? "";
        }
        catch
        {
            CurrentSubjectName = "";
        }
    }

    private void OnClassChanged()
    {
        RefreshCurrentSubject();
        ClassStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnBreakingTime()
    {
        CurrentSubjectName = "";
        ClassStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAfterSchool()
    {
        CurrentSubjectName = "";
        ClassStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnTimeStateChanged()
    {
        RefreshCurrentSubject();
        ClassStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
