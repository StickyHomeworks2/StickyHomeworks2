using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;

namespace StickyHomeworks2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public static readonly ExpandIconValueConverter ExpandIconConverter = new();
    public static readonly ExpandToolTipValueConverter ExpandToolTipConverter = new();
    public static readonly LockIconValueConverter LockIconConverter = new();
    public static readonly ColorToBrushConverter ColorToBrushConverter = new();

    [ObservableProperty]
    private string _title = "作业";

    [ObservableProperty]
    private bool _isUnlocked;

    [ObservableProperty]
    private bool _isFrozen;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isClosing;

    [ObservableProperty]
    private bool _isWorking;

    [ObservableProperty]
    private object? _selectedHomework;

    [ObservableProperty]
    private double _scale = 1.5;

    [ObservableProperty]
    private double _opacity = 0.7;

    [ObservableProperty]
    private Color _primaryColor = Color.FromRgb(34, 209, 236);

    [ObservableProperty]
    private Color _titleColor = Colors.White;

    [ObservableProperty]
    private double _maxPanelWidth = 350;

    [ObservableProperty]
    private bool _canRecoverExpireHomework;

    public ObservableCollection<HomeworkItem> Homeworks { get; } = new();

    [RelayCommand]
    private void ToggleLock() => IsUnlocked = !IsUnlocked;

    [RelayCommand]
    private void ToggleFreeze() => IsFrozen = !IsFrozen;

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private void CreateHomework()
    {
        Homeworks.Add(new HomeworkItem
        {
            Subject = "数学",
            Content = "ABCDEFG1234560",
            Tags = new ObservableCollection<string> { "重要" }
        });
    }

    [RelayCommand]
    private void Export()
    {
    }

    [RelayCommand]
    private void RecoverExpired()
    {
    }

    [RelayCommand]
    private void OpenTimeMachine()
    {
    }

    [RelayCommand]
    private void OpenSettings()
    {
    }

    [RelayCommand]
    private void Exit()
    {
        IsClosing = true;
    }
}

public class ExpandIconValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? MaterialIconKind.WindowMinimize : MaterialIconKind.WindowRestore;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ExpandToolTipValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? "收起主界面" : "展开主界面";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class LockIconValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? MaterialIconKind.LockOpen : MaterialIconKind.Lock;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ColorToBrushConverter : IValueConverter
{
    private static readonly Dictionary<uint, SolidColorBrush> Cache = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color c)
        {
            var key = (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
            if (!Cache.TryGetValue(key, out var brush))
            {
                brush = new SolidColorBrush(c);
                Cache[key] = brush;
            }
            return brush;
        }
        return value;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public partial class HomeworkItem : ObservableObject
{
    [ObservableProperty]
    private string _subject = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();
}
