using StickyHomeworks.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace StickyHomeworks.Controls;

public partial class ClassIslandSubjectsCard : UserControl
{
    public static readonly DependencyProperty SubjectsProperty =
        DependencyProperty.Register(nameof(Subjects), typeof(ObservableCollection<SubjectAction>), 
            typeof(ClassIslandSubjectsCard), new PropertyMetadata(new ObservableCollection<SubjectAction>()));

    public ObservableCollection<SubjectAction> Subjects
    {
        get => (ObservableCollection<SubjectAction>)GetValue(SubjectsProperty);
        set => SetValue(SubjectsProperty, value);
    }

    public event RoutedEventHandler? RefreshClicked;
    public event RoutedEventHandler? ImportClicked;

    public ClassIslandSubjectsCard()
    {
        InitializeComponent();
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshClicked?.Invoke(this, e);
    }

    private void ButtonImport_OnClick(object sender, RoutedEventArgs e)
    {
        ImportClicked?.Invoke(this, e);
    }
}