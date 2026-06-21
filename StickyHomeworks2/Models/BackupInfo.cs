using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace StickyHomeworks.Models;

// 备份信息模型：存储单个备份的元数据
public class BackupInfo : INotifyPropertyChanged
{
    public DateTime BackupTime { get; set; }
    public string ProfileFileName { get; set; } = "";
    public string PreviewImageFileName { get; set; } = "";
    private bool _isSelected;
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value == _isSelected) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}