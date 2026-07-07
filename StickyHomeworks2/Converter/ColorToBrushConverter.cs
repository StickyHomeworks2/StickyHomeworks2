using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Diagnostics;

namespace StickyHomeworks.Converter;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            // 调试输出
            Debug.WriteLine($"ColorToBrushConverter: 接收到值 = {value}, 类型 = {value?.GetType().Name}");

            if (value == null)
            {
                Debug.WriteLine("ColorToBrushConverter: 值为 null，返回默认黑色");
                return Brushes.Black;
            }

            // 处理Color类型
            if (value is Color color)
            {
                Debug.WriteLine($"ColorToBrushConverter: 转换为颜色 {color}");
                return new SolidColorBrush(color);
            }


            if (value is string colorString && !string.IsNullOrWhiteSpace(colorString))
            {
                try
                {
                    var convertedColor = (Color)ColorConverter.ConvertFromString(colorString);
                    Debug.WriteLine($"ColorToBrushConverter: 字符串 '{colorString}' 转换为颜色 {convertedColor}");
                    return new SolidColorBrush(convertedColor);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ColorToBrushConverter: 字符串转换失败: {ex.Message}");
                    return Brushes.Black;
                }
            }

            // 处理System.Drawing.Color
            if (value is System.Drawing.Color sysColor)
            {
                var wpfColor = Color.FromArgb(sysColor.A, sysColor.R, sysColor.G, sysColor.B);
                Debug.WriteLine($"ColorToBrushConverter: System.Drawing.Color 转换为 {wpfColor}");
                return new SolidColorBrush(wpfColor);
            }

            Debug.WriteLine($"ColorToBrushConverter: 无法处理类型 {value.GetType().Name}");
            return Brushes.Black;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ColorToBrushConverter: 转换时发生异常: {ex.Message}");
            return Brushes.Black;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }
}