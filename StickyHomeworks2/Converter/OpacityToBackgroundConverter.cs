using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StickyHomeworks.Converter;

public class OpacityToBackgroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is SolidColorBrush brush && values[1] is double opacity)
        {
            var color = brush.Color;
            return new SolidColorBrush(Color.FromArgb(
                (byte)(opacity * 255),
                color.R, color.G, color.B));
        }
        return values[0] ?? Brushes.White;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}