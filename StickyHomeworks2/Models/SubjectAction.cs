namespace StickyHomeworks.Models;

public enum SubjectActionMode
{
    HideInClass = 0,
    ShowInClass = 1,
    HideInClassShowAfter = 2,
    ShowInClassHideAfter = 3
}

public class SubjectAction
{
    public string Name { get; set; } = "";
    public bool IsMonitored { get; set; }
    public SubjectActionMode ActionMode { get; set; }

    public SubjectAction() { }

    public SubjectAction(string name, bool isMonitored = false, SubjectActionMode actionMode = SubjectActionMode.HideInClass)
    {
        Name = name;
        IsMonitored = isMonitored;
        ActionMode = actionMode;
    }
}