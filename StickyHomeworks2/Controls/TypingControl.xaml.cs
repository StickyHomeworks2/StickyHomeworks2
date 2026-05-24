using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StickyHomeworks.Controls;

public partial class TypingControl : UserControl
{
    public static readonly DependencyProperty DisplayingTextProperty = DependencyProperty.Register(
        nameof(DisplayingText), typeof(string), typeof(TypingControl), new PropertyMetadata(""));

    public string DisplayingText
    {
        get => (string)GetValue(DisplayingTextProperty);
        set => SetValue(DisplayingTextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(TypingControl), new PropertyMetadata("", (o, args) =>
        {
            if (o is TypingControl control)
                control.UpdateText();
        }));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy), typeof(bool), typeof(TypingControl), new PropertyMetadata(false));

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    private bool _isFirstUpdate = true;
    private int _updateSerial = 0;

    public TypingControl()
    {
        InitializeComponent();
    }

    private async void UpdateText()
    {
        var mySerial = ++_updateSerial;
        IsBusy = true;
        if (!_isFirstUpdate)
        {
            DisplayingText = "";
            await Task.Delay(TimeSpan.FromMilliseconds(150));
            var text = Text;
            for (int i = 0; i < text.Length; i++)
            {
                if (_updateSerial != mySerial) return;
                DisplayingText = string.Concat(text.AsSpan(0, i), (i / 10) % 2 == 0 ? "_" : "");
                await Task.Delay(TimeSpan.FromMilliseconds(40));
            }
        }
        _isFirstUpdate = false;
        if (_updateSerial != mySerial) return;
        DisplayingText = Text;
        IsBusy = false;
    }
}
