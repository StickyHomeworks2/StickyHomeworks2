using System.ComponentModel;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.IPC;
using ClassIsland.Shared.IPC.Abstractions.Services;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies;
using Microsoft.Extensions.Hosting;

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
        if (_settingsService.Settings.IsClassIslandIpcEnabled)
            _ = ConnectWithRetryAsync(cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await ConnectAsync();
                if (IsConnected) return;
            }
            catch
            {
                // ignored
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ = DisconnectAsync();
        return Task.CompletedTask;
    }

    public async Task ConnectAsync()
    {
        try
        {
            await DisconnectAsync();

            _ipcClient = new IpcClient();
            _ipcClient.JsonIpcProvider.AddNotifyHandler(IpcRoutedNotifyIds.CurrentTimeStateChangedNotifyId, OnTimeStateChanged);
            // 参考官方 Demo: 不使用 await
            _ = _ipcClient.Connect();

            // 等待连接完成
            await Task.Delay(1000);

            _lessonsService = _ipcClient.Provider.CreateIpcProxy<IPublicLessonsService>(_ipcClient.PeerProxy!);
            IsConnected = true;
            ConnectionStatus = "已连接";

            RefreshState();
            StartPolling();
        }
        catch
        {
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
        _ipcClient = null;
        _lessonsService = null;
        IsConnected = false;
        ConnectionStatus = "未连接";
        CurrentState = TimeState.None;
        CurrentSubjectName = "";

        _pollingCts?.Cancel();
        _pollingCts = null;

        await Task.Delay(100);
    }

    private void StartPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts = new CancellationTokenSource();
        _ = PollingLoopAsync(_pollingCts.Token);
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
                    ClassStateChanged?.Invoke(this, EventArgs.Empty);
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