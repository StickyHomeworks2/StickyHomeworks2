using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;

namespace StickyHomeworks.Services;
public class TimeMachineService : IHostedService, INotifyPropertyChanged
{
    private const string BackupDir = "./backups";
    private const string IndexFile = "backup_index.json";
    private const int MaxBackupCount = 20;
    
    private readonly ProfileService _profileService;
    private ObservableCollection<BackupInfo> _backups = new();
    private bool _isRestoring;

    public ObservableCollection<BackupInfo> Backups
    {
        get => _backups;
        set
        {
            if (Equals(value, _backups)) return;
            _backups = value;
            OnPropertyChanged();
        }
    }
    public bool IsRestoring
    {
        get => _isRestoring;
        set
        {
            if (value == _isRestoring) return;
            _isRestoring = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler? BackupCreated;
    public event EventHandler? RestoreCompleted;
    public event PropertyChangedEventHandler? PropertyChanged;

    public TimeMachineService(ProfileService profileService)
    {
        _profileService = profileService;
        LoadBackupIndex();
    }
    public void CreateBackup(ListView? listView)
    {
        try
        {
            EnsureBackupDirExists();

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupId = Guid.NewGuid().ToString("N")[..8];
            var profileFileName = $"profile_{timestamp}_{backupId}.json";
            var previewFileName = $"preview_{timestamp}_{backupId}.png";
            if (File.Exists("./Profile.json"))
            {
                File.Copy("./Profile.json", Path.Combine(BackupDir, profileFileName), true);
            }
            if (listView is { ActualWidth: > 0, ActualHeight: > 0 })
            {
                GeneratePreviewImage(listView, Path.Combine(BackupDir, previewFileName));
            }
            Backups.Insert(0, new BackupInfo
            {
                BackupTime = DateTime.Now,
                ProfileFileName = profileFileName,
                PreviewImageFileName = previewFileName
            });
            while (Backups.Count > MaxBackupCount)
            {
                RemoveBackupFiles(Backups[^1]);
                Backups.RemoveAt(Backups.Count - 1);
            }

            SaveBackupIndex();
            BackupCreated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建备份失败: {ex.Message}");
        }
    }
    private void GeneratePreviewImage(ListView listView, string outputPath)
    {
        try
        {
            const double scale = 1.5;
            var width = (int)(listView.ActualWidth * scale + 100);
            var height = (int)(listView.ActualHeight * scale + 100);

            var finalVisual = new DrawingVisual();
            using (var ctx = finalVisual.RenderOpen())
            {
                var bg = Application.Current.FindResource("MaterialDesignPaper") as Brush ?? Brushes.White;
                ctx.DrawRectangle(bg, null, new Rect(0, 0, width, height));
                var brush = new VisualBrush(listView) { Stretch = Stretch.None };
                ctx.DrawRectangle(brush, null, new Rect(50, 50, listView.ActualWidth * scale, listView.ActualHeight * scale));
            }

            var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
            bitmap.Render(finalVisual);
            
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using var stream = new FileStream(outputPath, FileMode.Create);
            encoder.Save(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"生成预览图失败: {ex.Message}");
        }
    }
    public async Task RestoreBackup(BackupInfo backup)
    {
        try
        {
            IsRestoring = true;
            
            var backupPath = Path.Combine(BackupDir, backup.ProfileFileName);
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("备份文件不存在");
            }

            var json = await File.ReadAllTextAsync(backupPath);
            var profile = JsonSerializer.Deserialize<Profile>(json) ?? throw new InvalidDataException("备份数据格式无效");

            _profileService.Profile = profile;
            _profileService.SaveProfile();

            await Task.Delay(100);
            IsRestoring = false;
            RestoreCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            IsRestoring = false;
            throw;
        }
    }    
    public void RemoveBackup(BackupInfo backup)
    {
        RemoveBackupFiles(backup);
        Backups.Remove(backup);
        SaveBackupIndex();
    }
    public void ClearAllBackups()
    {
        foreach (var backup in Backups.ToList())
        {
            RemoveBackup(backup);
        }
    }
    public string GetPreviewImagePath(BackupInfo backup) => Path.Combine(BackupDir, backup.PreviewImageFileName);

    private void EnsureBackupDirExists()
    {
        if (!Directory.Exists(BackupDir))
        {
            Directory.CreateDirectory(BackupDir);
        }
    }

    private void RemoveBackupFiles(BackupInfo backup)
    {
        try
        {
            var profilePath = Path.Combine(BackupDir, backup.ProfileFileName);
            var previewPath = Path.Combine(BackupDir, backup.PreviewImageFileName);
            if (File.Exists(profilePath)) File.Delete(profilePath);
            if (File.Exists(previewPath)) File.Delete(previewPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"删除备份文件失败: {ex.Message}");
        }
    }

    private void LoadBackupIndex()
    {
        try
        {
            var indexPath = Path.Combine(BackupDir, IndexFile);
            if (!File.Exists(indexPath)) return;

            var json = File.ReadAllText(indexPath);
            var backups = JsonSerializer.Deserialize<ObservableCollection<BackupInfo>>(json);
            if (backups == null) return;

            Backups = backups;
            for (int i = Backups.Count - 1; i >= 0; i--)
            {
                if (!File.Exists(Path.Combine(BackupDir, Backups[i].ProfileFileName)))
                {
                    Backups.RemoveAt(i);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载备份索引失败: {ex.Message}");
        }
    }

    private void SaveBackupIndex()
    {
        try
        {
            EnsureBackupDirExists();
            File.WriteAllText(Path.Combine(BackupDir, IndexFile), JsonSerializer.Serialize(Backups));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存备份索引失败: {ex.Message}");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
