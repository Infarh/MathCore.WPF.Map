using System.ComponentModel;
using System.Globalization;

namespace MathCore.WPF.Map.Projections.Base;

public class MapProjectionConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type SourceType) => SourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string str)
            throw new ArgumentException("Значение должно быть строкой", nameof(value));

        switch (str.ToLower())
        {
            default: throw new InvalidOperationException($"Тип проекции {str} не поддерживается");

            case "3857":
            case "epsg:3857":
            case "web":
            case "webmercator":
            case "mercator":
                return new WebMercatorProjection();

            case "3395":
            case "epsg:3395":
            case "world":
            case "worldmercator":
                return new WorldMercatorProjection();

            case "epsg:4326":
                return new EquirectangularProjection();

            case "yandex":
            case "яндекс":
                return new YandexProjection();
        }
    }
}