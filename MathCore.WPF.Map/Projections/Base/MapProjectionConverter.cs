using System.ComponentModel;
using System.Globalization;

namespace MathCore.WPF.Map.Projections.Base;

/// <summary>Конвертер типов для преобразования строковых идентификаторов проекций карты в соответствующие объекты проекций</summary>
/// <example>
/// <code>
/// <![CDATA[
/// <MapControl Projection="WebMercator" />
/// <MapControl Projection="3857" />
/// <MapControl Projection="EPSG:3857" />
/// <MapControl Projection="Yandex" />
/// ]]>
/// </code>
/// </example>
public class MapProjectionConverter : TypeConverter
{
    /// <summary>Определяет, может ли конвертер преобразовать объект указанного типа</summary>
    /// <param name="context">Контекст дескриптора типа</param>
    /// <param name="SourceType">Тип источника преобразования</param>
    /// <returns>Истина, если тип источника является строкой; иначе ложь</returns>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type SourceType) => SourceType == typeof(string);

    /// <summary>Преобразует строковый идентификатор проекции в соответствующий объект проекции карты</summary>
    /// <param name="context">Контекст дескриптора типа</param>
    /// <param name="culture">Информация о культуре</param>
    /// <param name="value">Строковый идентификатор проекции (например, "3857", "EPSG:3857", "WebMercator", "Yandex")</param>
    /// <returns>Объект проекции карты, соответствующий указанному идентификатору</returns>
    /// <exception cref="ArgumentException">Значение не является строкой</exception>
    /// <exception cref="InvalidOperationException">Указанный тип проекции не поддерживается</exception>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string str)
            throw new ArgumentException("Значение должно быть строкой", nameof(value));

        return str.ToLower() switch
        {
            "3857" or "epsg:3857" or "web" or "webmercator" or "mercator" => new WebMercatorProjection(),
            "3395" or "epsg:3395" or "world" or "worldmercator" => new WorldMercatorProjection(),
            "epsg:4326" => new EquirectangularProjection(),
            "yandex" or "яндекс" => new YandexProjection(),
            _ => throw new InvalidOperationException($"Тип проекции {str} не поддерживается"),
        };
    }
}