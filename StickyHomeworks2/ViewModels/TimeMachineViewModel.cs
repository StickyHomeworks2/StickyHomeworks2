using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StickyHomeworks.Models;
using StickyHomeworks.Services;

namespace StickyHomeworks.ViewModels;
public class TimeMachineViewModel : ObservableRecipient
{
    private readonly TimeMachineService _timeMachineService;
    private BackupInfo? _selectedBackup;
    private bool _isWorking;
    private bool _isConfirmDialogOpen;
    private bool _isDeleteDialogOpen;
    private bool _isClearAllDialogOpen;
    private string _statusMessage = "";

    public ObservableCollection<BackupInfo> Backups => _timeMachineService.Backups;
    public BackupInfo? SelectedBackup
    {
        get => _selectedBackup;
        set => SetProperty(ref _selectedBackup, value);
    }  
    public bool IsWorking
    {
        get => _isWorking;
        set => SetProperty(ref _isWorking, value);
    }
    public bool IsConfirmDialogOpen
    {
        get => _isConfirmDialogOpen;
        set => SetProperty(ref _isConfirmDialogOpen, value);
    }
    public bool IsDeleteDialogOpen
    {
        get => _isDeleteDialogOpen;
        set => SetProperty(ref _isDeleteDialogOpen, value);
    }
    public bool IsClearAllDialogOpen
    {
        get => _isClearAllDialogOpen;
        set => SetProperty(ref _isClearAllDialogOpen, value);
    }
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public TimeMachineViewModel(TimeMachineService timeMachineService)
    {
        _timeMachineService = timeMachineService;
    }
    public async Task RestoreSelectedBackupAsync()
    {
        if (SelectedBackup == null) return;

        try
        {
            IsWorking = true;
            StatusMessage = "正在还原...";
            
            await _timeMachineService.RestoreBackup(SelectedBackup);
            
            StatusMessage = $"已成功还原到 {SelectedBackup.BackupTime:yyyy-MM-dd HH:mm:ss}";
            await Task.Delay(2000);
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"还原失败: {ex.Message}";
        }
        finally
        {
            IsWorking = false;
            IsConfirmDialogOpen = false;
        }
    }
    public void DeleteBackup(BackupInfo backup)
    {
        if (backup == null) return;
        
        if (SelectedBackup == backup)
        {
            SelectedBackup = null;
        }
        
        _timeMachineService.RemoveBackup(backup);
    }
    public void DeleteSelectedBackup()
    {
        if (SelectedBackup == null) return;
        
        _timeMachineService.RemoveBackup(SelectedBackup);
        SelectedBackup = null;
    }
    public void ClearAllBackups()
    {
        _timeMachineService.ClearAllBackups();
        SelectedBackup = null;
    }
    public void ShowConfirmDialog() => IsConfirmDialogOpen = SelectedBackup != null;
    public void CancelConfirmDialog() => IsConfirmDialogOpen = false;
    public void ShowDeleteDialog() => IsDeleteDialogOpen = SelectedBackup != null;
    public void CancelDeleteDialog() => IsDeleteDialogOpen = false;
    public void ShowClearAllDialog() => IsClearAllDialogOpen = true;
    public void CancelClearAllDialog() => IsClearAllDialogOpen = false;
}
