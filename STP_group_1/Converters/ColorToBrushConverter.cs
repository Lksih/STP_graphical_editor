using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace STP_group_1.Converters;

public sealed class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color c)
            return new SolidColorBrush(c);
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}


