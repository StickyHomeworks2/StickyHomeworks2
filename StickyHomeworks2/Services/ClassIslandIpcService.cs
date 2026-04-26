using System.ComponentModel;
using ClassIsland.Shared.Enums;
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
    private TimeState _currentState;
    private string _currentSubjectName = "";
    private string _previousSubjectName = "";
    private bool _isConnected;
    private string _connectionStatus = "未连接";
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ClassStateChanged;

    public TimeState CurrentState
    {
        get => _currentState;
        private set
        {
            if (value == _currentState) return;
            _currentState = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentState)));
        }
    }

    public string CurrentSubjectName
    {
        get => _currentSubjectName;
        private set
        {
            if (value == _currentSubjectName) return;
            _previousSubjectName = _currentSubjectName;
            _currentSubjectName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSubjectName)));
        }
    }

    public string PreviousSubjectName => _previousSubjectName;

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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"[IPC] StartAsync: IsClassIslandIpcEnabled={_settingsService.Settings.IsClassIslandIpcEnabled}");
        if (_settingsService.Settings.IsClassIslandIpcEnabled)
        {
            _ = Task.Run(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[IPC] StartAsync: 开始连接任务...");
                await ConnectAsync();
                System.Diagnostics.Debug.WriteLine($"[IPC] StartAsync: 连接完成, IsConnected={IsConnected}");
            }, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisconnectAsync();
    }

    public async Task ConnectAsync()
    {
        System.Diagnostics.Debug.WriteLine("[IPC] ConnectAsync: 开始连接...");
        try
        {
            await DisconnectAsync();
            
            _ipcClient = new IpcClient();
            // 只监听 CurrentTimeStateChangedNotifyId 事件
            _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.CurrentTimeStateChangedNotifyId, OnTimeStateChanged);
            
            // 参考 Demo: 不使用 await
            _ = _ipcClient.Connect();
            
            // 等待一段时间后获取服务
            await Task.Delay(500);
            
            _lessonsService = _ipcClient.Provider.CreateIpcProxy<IPublicLessonsService>(_ipcClient.PeerProxy!);
            IsConnected = true;
            ConnectionStatus = "已连接";
            System.Diagnostics.Debug.WriteLine("[IPC] ConnectAsync: 连接成功");
            RefreshState();
            StartPolling();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IPC] ConnectAsync: 连接失败 - {ex}");
            IsConnected = false;
            ConnectionStatus = "连接失败";
        }
    }

    public void Disconnect()
    {
        _ = DisconnectAsync();
    }

    private async Task DisconnectAsync()
    {
        var oldClient = _ipcClient;
        _ipcClient = null;
        _lessonsService = null;
        IsConnected = false;
        ConnectionStatus = "未连接";
        CurrentState = TimeState.None;
        CurrentSubjectName = "";
        
        _pollingCts?.Cancel();
        _pollingCts = null;
        
        if (oldClient != null)
        {
            await Task.Delay(100);
        }
    }

    private void StartPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();
        _pollingTask = PollingLoopAsync(_pollingCts.Token);
    }

    private async Task PollingLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(2000, token);
                if (_ipcClient == null) break;
                
                var oldState = _currentState;
                var oldSubject = _currentSubjectName;
                RefreshState();
                
                if (_currentState != oldState || _currentSubjectName != oldSubject)
                {
                    ClassStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
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

    public void RefreshState()
    {
        if (_lessonsService == null) return;
        try
        {
            CurrentState = _lessonsService.CurrentState;
            // 过滤掉 "???" 无效科目名
            var name = _lessonsService.CurrentSubject?.Name ?? "";
            if (name == "???") name = "";
            CurrentSubjectName = name;
        }
        catch
        {
            CurrentState = TimeState.None;
            CurrentSubjectName = "";
        }
    }

    private void OnTimeStateChanged()
    {
        RefreshState();
        ClassStateChanged?.Invoke(this, EventArgs.Empty);
    }
}