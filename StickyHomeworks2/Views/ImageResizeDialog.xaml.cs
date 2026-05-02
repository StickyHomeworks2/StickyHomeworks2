using System.Windows;
using System.Windows.Controls;

namespace StickyHomeworks.Views;

public partial class ImageResizeDialog : Window
{
    public double ZoomPercent { get; private set; }
    private readonly double _initialZoom;
    private readonly Action<double> _onZoomChanged;
    private readonly Action _onCancel;

    public ImageResizeDialog(double initialZoom, Action<double> onZoomChanged, Action onCancel)
    {
        InitializeComponent();
        _initialZoom = initialZoom;
        _onZoomChanged = onZoomChanged;
        _onCancel = onCancel;
        ZoomPercent = initialZoom;
        ZoomSlider.Value = initialZoom;
        ZoomPercentText.Text = $"{initialZoom:F0}%";
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ZoomPercent = e.NewValue;
        ZoomPercentText.Text = $"{ZoomPercent:F0}%";
        _onZoomChanged?.Invoke(ZoomPercent);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _onCancel?.Invoke();
        DialogResult = false;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (DialogResult != true)
        {
            _onCancel?.Invoke();
        }
        base.OnClosing(e);
    }
}
