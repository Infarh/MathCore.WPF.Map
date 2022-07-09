using System.Globalization;
using System.Windows.Markup;

using MathCore.WPF.Converters.Base;

namespace MathCore.WPF.Map.TestWPF.Converters;

[MarkupExtensionReturnType(typeof(LengthConverter))]
public class LengthConverter : ValueConverter
{
    protected override object? Convert(object? v, Type t, object? p, CultureInfo c)
    {
        var value = v switch
        {
            double x => x,
            string str => double.Parse(str),
            null => double.NaN,
            _ => throw new NotSupportedException($"Тип {v.GetType()} не поддерживается")
        };

        return value switch
        {
            double.NaN => null,
            < 0.9 => $"{value:f2} м",
            _ => $"{value / 1000:f2} км"
        };
    }
}
