using System.ComponentModel;

namespace StickyHomeworks.Models;

public class SubjectAction : INotifyPropertyChanged
{
    private string _name = "";
    private bool _isMonitored;
    private int _actionMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public bool IsMonitored
    {
        get => _isMonitored;
        set { _isMonitored = value; OnPropertyChanged(nameof(IsMonitored)); }
    }

    /// <summary>
    /// 0=上课时隐藏, 1=上课时显示, 2=上课时隐藏&下课时显示, 3=上课时显示&下课时隐藏
    /// </summary>
    public int ActionMode
    {
        get => _actionMode;
        set { _actionMode = value; OnPropertyChanged(nameof(ActionMode)); }
    }

    public SubjectAction() { }

    public SubjectAction(string name, bool isMonitored = false, int actionMode = 0)
    {
        Name = name;
        IsMonitored = isMonitored;
        ActionMode = actionMode;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}