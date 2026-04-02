using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StickyHomeworks.Models;
using StickyHomeworks.ViewModels;

namespace StickyHomeworks.Views;
public partial class TimeMachineWindow
{
    public TimeMachineViewModel ViewModel { get; }
    public bool IsOpened { get; set; }

    public TimeMachineWindow(TimeMachineViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
        
        IsVisibleChanged += (_, _) => { if (IsVisible) RefreshBackupList(); };
        Closing += OnWindowClosing;
    }
    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        IsOpened = false;
    }
    private void RefreshBackupList()
    {
        foreach (var backup in ViewModel.Backups)
        {
            backup.IsSelected = false;
        }
        ViewModel.SelectedBackup = null;
    }

    #region 备份项交互事件
    private void BackupItem_OnClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { DataContext: BackupInfo clickedBackup }) return;
        if (ViewModel.SelectedBackup != null)
        {
            ViewModel.SelectedBackup.IsSelected = false;
        }
        if (ViewModel.SelectedBackup == clickedBackup)
        {
            ViewModel.SelectedBackup = null;
        }
        else
        {
            clickedBackup.IsSelected = true;
            ViewModel.SelectedBackup = clickedBackup;
        }
    }
    private async void ButtonRestore_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupInfo backup }) return;
        
        ViewModel.SelectedBackup = backup;
        await ViewModel.RestoreSelectedBackupAsync();
    }
    private void ButtonDeleteBackup_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupInfo backup }) return;
        
        ViewModel.SelectedBackup = backup;
        ViewModel.ShowDeleteDialog();
    }

    #endregion

    #region 底部操作栏按钮事件
    private void ButtonDeleteSelected_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBackup == null) return;
        ViewModel.ShowDeleteDialog();
    }
    private void ButtonRestoreSelected_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedBackup != null)
        {
            ViewModel.ShowConfirmDialog();
        }
    }
    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
        IsOpened = false;
    }

    /// 清空所有备份按钮
    private void ButtonClearAll_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowClearAllDialog();
    }

    #endregion

    #region 对话框按钮事件
    private void ButtonConfirmCancel_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelConfirmDialog();
    }
    private async void ButtonConfirmRestore_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelConfirmDialog();
        await Task.Delay(100); 
        await ViewModel.RestoreSelectedBackupAsync();
    }
    private void ButtonDeleteCancel_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelDeleteDialog();
    }
    private void ButtonDeleteConfirm_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelDeleteDialog();
        ViewModel.DeleteSelectedBackup();
    }
    private void ButtonClearAllCancel_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelClearAllDialog();
    }
    private void ButtonClearAllConfirm_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelClearAllDialog();
        ViewModel.ClearAllBackups();
    }

    #endregion
}
