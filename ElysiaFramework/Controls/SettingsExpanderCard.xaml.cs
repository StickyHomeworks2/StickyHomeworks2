using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace ElysiaFramework.Controls;

public partial class SettingsExpanderCard : UserControl
{
    private bool _isUpdatingFromIsOn;

    public static readonly DependencyProperty IconGlyphProperty = DependencyProperty.Register(
        nameof(IconGlyph), typeof(PackIconKind), typeof(SettingsExpanderCard), new PropertyMetadata(PackIconKind.SimpleIcons));

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(SettingsExpanderCard), new PropertyMetadata(""));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(SettingsExpanderCard), new PropertyMetadata(""));

    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
        nameof(IsOn), typeof(bool), typeof(SettingsExpanderCard), new PropertyMetadata(false, OnIsOnChanged));

    public static readonly DependencyProperty HasSwitcherProperty = DependencyProperty.Register(
        nameof(HasSwitcher), typeof(bool), typeof(SettingsExpanderCard), new PropertyMetadata(true));

    public static readonly DependencyProperty ContentIconGlyphProperty = DependencyProperty.Register(
        nameof(ContentIconGlyph), typeof(PackIconKind), typeof(SettingsExpanderCard), new PropertyMetadata(PackIconKind.SimpleIcons));

    public static readonly DependencyProperty ContentHeaderProperty = DependencyProperty.Register(
        nameof(ContentHeader), typeof(string), typeof(SettingsExpanderCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ContentDescriptionProperty = DependencyProperty.Register(
        nameof(ContentDescription), typeof(string), typeof(SettingsExpanderCard), new PropertyMetadata(""));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(SettingsExpanderCard), new PropertyMetadata(null));

    private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SettingsExpanderCard)d;
        control._isUpdatingFromIsOn = true;
        if (control.PART_Expander != null)
        {
            control.PART_Expander.IsExpanded = (bool)e.NewValue;
        }
        control._isUpdatingFromIsOn = false;
    }

    public PackIconKind IconGlyph
    {
        get => (PackIconKind)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public bool HasSwitcher
    {
        get => (bool)GetValue(HasSwitcherProperty);
        set => SetValue(HasSwitcherProperty, value);
    }

    public PackIconKind ContentIconGlyph
    {
        get => (PackIconKind)GetValue(ContentIconGlyphProperty);
        set => SetValue(ContentIconGlyphProperty, value);
    }

    public string ContentHeader
    {
        get => (string)GetValue(ContentHeaderProperty);
        set => SetValue(ContentHeaderProperty, value);
    }

    public string ContentDescription
    {
        get => (string)GetValue(ContentDescriptionProperty);
        set => SetValue(ContentDescriptionProperty, value);
    }

    public new object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public SettingsExpanderCard()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (PART_Expander != null)
        {
            PART_Expander.IsExpanded = IsOn;
        }
    }
}
