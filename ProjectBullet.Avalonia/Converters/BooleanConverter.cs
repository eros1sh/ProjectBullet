using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ProjectBullet.Avalonia.Converters;

public class BooleanConverter<T> : IValueConverter
{
    public BooleanConverter(T trueValue, T falseValue)
    {
        True = trueValue;
        False = falseValue;
    }

    public T True { get; set; }
    public T False { get; set; }

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? True : False;

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is T convertedValue && EqualityComparer<T>.Default.Equals(convertedValue, True);
}

public sealed class BoolToVisibilityConverter : BooleanConverter<bool>
{
    public BoolToVisibilityConverter() :
        base(true, false)
    {

    }
}

public sealed class BoolToThicknessConverter : BooleanConverter<Thickness>
{
    public BoolToThicknessConverter() :
        base(new Thickness(1), new Thickness(0))
    {

    }
}

public sealed class BoolToTextWrappingConverter : BooleanConverter<TextWrapping>
{
    public BoolToTextWrappingConverter() :
        base(TextWrapping.Wrap, TextWrapping.NoWrap)
    {

    }
}

public sealed class BoolToColorConverter : IValueConverter
{
    public string True { get; set; } = "Gold";
    public string False { get; set; } = "Gray";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var colorName = value is bool b && b ? True : False;
        var color = Color.Parse(colorName);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
