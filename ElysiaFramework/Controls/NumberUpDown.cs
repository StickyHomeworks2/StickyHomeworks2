using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace ElysiaFramework.Controls;

public class NumberUpDown : TemplatedControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<NumberUpDown, double>(nameof(Value), 1.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumberUpDown, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumberUpDown, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<double> IncrementProperty =
        AvaloniaProperty.Register<NumberUpDown, double>(nameof(Increment), 0.05);

    public static readonly StyledProperty<string> FormatStringProperty =
        AvaloniaProperty.Register<NumberUpDown, string>(nameof(FormatString), "F2");

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    private Button? _decreaseButton;
    private Button? _increaseButton;
    private TextBlock? _valueText;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _decreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
        _increaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");
        _valueText = e.NameScope.Find<TextBlock>("PART_ValueText");

        if (_decreaseButton is not null)
            _decreaseButton.Click += OnDecreaseClick;
        if (_increaseButton is not null)
            _increaseButton.Click += OnIncreaseClick;

        UpdateValueDisplay();
    }

    private void OnIncreaseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var newValue = Math.Min(Maximum, Value + Increment);
        if (!double.IsNaN(newValue) && Math.Abs(newValue - Value) > 1e-9)
        {
            Value = newValue;
            UpdateValueDisplay();
        }
    }

    private void OnDecreaseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var newValue = Math.Max(Minimum, Value - Increment);
        if (!double.IsNaN(newValue) && Math.Abs(newValue - Value) > 1e-9)
        {
            Value = newValue;
            UpdateValueDisplay();
        }
    }

    private void UpdateValueDisplay()
    {
        if (_valueText is not null)
            _valueText.Text = Value.ToString(FormatString, CultureInfo.CurrentCulture);
    }
}
