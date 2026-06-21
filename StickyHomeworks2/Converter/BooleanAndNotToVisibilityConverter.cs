using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StickyHomeworks.Converter;

public class BooleanAndNotToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            values[0] is bool a &&
            values[1] is bool b)
        {
            return a && !b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}
