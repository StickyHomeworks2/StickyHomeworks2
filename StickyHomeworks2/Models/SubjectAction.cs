using System.ComponentModel;

namespace StickyHomeworks.Models;

public enum SubjectActionMode
{
    HideInClass = 0,
    ShowInClass = 1,
    HideInClassShowAfter = 2,
    ShowInClassHideAfter = 3
}

public class SubjectAction : INotifyPropertyChanged
{
    private string _name = "";
    private bool _isMonitored;
    private SubjectActionMode _actionMode;

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

    public SubjectActionMode ActionMode
    {
        get => _actionMode;
        set { _actionMode = value; OnPropertyChanged(nameof(ActionMode)); }
    }

    public SubjectAction() { }

    public SubjectAction(string name, bool isMonitored = false, SubjectActionMode actionMode = SubjectActionMode.HideInClass)
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