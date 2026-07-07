using System.Globalization;
using System.Windows.Data;

namespace StickyHomeworks.Converters;

public class MinValueMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var validValues = values.OfType<double>().Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToList();
        if (validValues.Count == 0)
            return double.NaN;
        return validValues.Min();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new object[] { };
    }
}