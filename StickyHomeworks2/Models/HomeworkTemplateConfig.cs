using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StickyHomeworks.Models;

public class HomeworkTemplateConfig
{
    public ObservableCollection<string> QuickActions { get; set; } = new();

    public Dictionary<string, ObservableCollection<string>> CommonBooks { get; set; } = new();

    public Dictionary<string, Dictionary<string, ObservableCollection<string>>> SubjectBooks { get; set; } = new();

    public void PruneSubjectBooksNotInSubjects(IEnumerable<string> subjects)
    {
        var set = new HashSet<string>(subjects);
        foreach (var key in SubjectBooks.Keys.ToList())
        {
            if (!set.Contains(key))
                SubjectBooks.Remove(key);
        }
    }

    public static void Normalize(HomeworkTemplateConfig? t)
    {
        if (t == null)
            return;
        t.QuickActions ??= new ObservableCollection<string>();
        t.CommonBooks ??= new Dictionary<string, ObservableCollection<string>>();
        t.SubjectBooks ??= new Dictionary<string, Dictionary<string, ObservableCollection<string>>>();

        foreach (var key in t.CommonBooks.Keys.ToList())
        {
            if (t.CommonBooks[key] == null)
                t.CommonBooks[key] = new ObservableCollection<string>();
        }

        foreach (var subjectKey in t.SubjectBooks.Keys.ToList())
        {
            var inner = t.SubjectBooks[subjectKey];
            if (inner == null)
            {
                t.SubjectBooks[subjectKey] = new Dictionary<string, ObservableCollection<string>>();
                continue;
            }

            foreach (var bookKey in inner.Keys.ToList())
            {
                if (inner[bookKey] == null)
                    inner[bookKey] = new ObservableCollection<string>();
            }
        }
    }
}
