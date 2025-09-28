using CommunityToolkit.Mvvm.ComponentModel;


namespace StickyHomeworks2.ViewModels;

public class ClashViewModel : ObservableRecipient
{
    private object? _drawerContent;


    public object? DrawerContent
    {
        get => _drawerContent;
        set
        {
            if (Equals(value, _drawerContent)) return;
            _drawerContent = value;
            OnPropertyChanged();
        }
    }
}
