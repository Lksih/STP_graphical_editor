using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace STP_group_1.Converters;

public sealed class BoolToEyeGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? "👁" : "🙈";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

