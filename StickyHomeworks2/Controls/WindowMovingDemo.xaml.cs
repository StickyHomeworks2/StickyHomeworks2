using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace StickyHomeworks.Controls;

public partial class WindowMovingDemo : UserControl
{
    private Storyboard? _loop;

    public WindowMovingDemo()
    {
        InitializeComponent();
    }

    private void UpdateAnimation()
    {
        _loop ??= (Storyboard)FindResource("Loop");
        
        if (IsVisible && IsLoaded)
        {
            _loop.Remove(this);
            _loop.Begin(this);
        }
        else
        {
            _loop?.Stop(this);
            _loop?.Remove(this);
        }
    }

    private void WindowMovingDemo_OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateAnimation();
    }

    private void WindowMovingDemo_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateAnimation();
    }
}