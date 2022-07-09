using System.ComponentModel;
using System.Globalization;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;

using Microsoft.Extensions.Primitives;

namespace MathCore.WPF.Map.Primitives;

/// <summary>Географическая прямоугольная область</summary>
[Serializable]
[TypeConverter(typeof(BoundingBoxConverter))]
public class BoundingBox : IEquatable<BoundingBox?>
{
    /// <summary>Северная граница</summary>
    private double _North;

    /// <summary>Восточная граница</summary>
    private double _East;

    /// <summary>Южная граница</summary>
    private double _South;

    /// <summary>Западная граница</summary>
    private double _West;

    /// <summary>Северная (верхняя) граница</summary>
    public double North
    {
        get => _North;
        set => _North = Math.Min(Math.Max(value, -90d), 90d);
    }

    /// <summary>Восточная (правая) граница</summary>
    public double East
    {
        get => _East;
        set => _East = value;
    }

    /// <summary>Южная (нижняя) граница</summary>
    public double South
    {
        get => _South;
        set => _South = Math.Min(Math.Max(value, -90d), 90d);
    }

    /// <summary>Западная (левая) граница</summary>
    public double West
    {
        get => _West;
        set => _West = value;
    }

    /// <summary>ШИрина области по долготе</summary>
    public virtual double Width => _East - _West;

    /// <summary>Ширина области по широте</summary>
    public virtual double Height => _North - _South;

    /// <summary>Область имеет корректные границы</summary>
    public bool HasValidBounds => _South < _North && _West < _East;

    /// <summary>Новая пустая прямоугольная область</summary>
    public BoundingBox() { }

    /// <summary>Новая прямоугольная область, заданная своими географическими координатами границ</summary>
    /// <param name="north">Северная (верхняя) граница</param>
    /// <param name="east">Восточная (правая) граница</param>
    /// <param name="south">Южная (нижняя) граница</param>
    /// <param name="west">Западная (левая) граница</param>
    public BoundingBox(double north, double east, double south, double west)
    {
        North = north;
        East = east;
        South = south;
        West = west;
    }

    public BoundingBox(Location Center, double Width, double Height)
    {
        var (lat, lon) = Center;
        var w2 = Width / 2;
        var h2 = Height / 2;

        North = lat + h2;
        East = lon + w2;
        West = lon - w2;
        South = lat - h2;
    }

    /// <summary>Клонирование области</summary>
    public virtual BoundingBox Clone() => new(_North, _East, _South, _West);

    public Location GetCenter() => new((_North + _South) / 2, (_East + _West) / 2);

    /// <summary>Разделители строки</summary>
    private static readonly char[] __Separators = { ' ', ',', ';', ':' };

    /// <summary>Формирование области из строки</summary>
    /// <param name="s">Строка с информацией об области</param>
    /// <exception cref="FormatException">При нарушении формата строки</exception>
    public static BoundingBox Parse(string s)
    {
        const NumberStyles number_styles = NumberStyles.Float;
        var culture = CultureInfo.InvariantCulture;

        var items = new StringSegment(s).Split(__Separators);

        using var token = items.GetEnumerator();

        if (!token.MoveNext())
            throw new FormatException("BoundingBox: ошибка чтения данных из строки географической области");
#if NET5_0_OR_GREATER
        var north = double.Parse(token.Current, number_styles, culture);
#else
        var north = double.Parse(token.Current.ToString(), number_styles, culture);
#endif

        if (!token.MoveNext())
            throw new FormatException("BoundingBox: ошибка чтения данных из строки географической области");
#if NET5_0_OR_GREATER
        var east = double.Parse(token.Current, number_styles, culture);
#else
        var east = double.Parse(token.Current.ToString(), number_styles, culture);
#endif

        if (!token.MoveNext())
            throw new FormatException("BoundingBox: ошибка чтения данных из строки географической области");
#if NET5_0_OR_GREATER
        var south = double.Parse(token.Current, number_styles, culture);
#else
        var south = double.Parse(token.Current.ToString(), number_styles, culture);
#endif

        if (!token.MoveNext())
            throw new FormatException("BoundingBox: ошибка чтения данных из строки географической области");
#if NET5_0_OR_GREATER
        var west = double.Parse(token.Current, number_styles, culture);
#else
        var west = double.Parse(token.Current.ToString(), number_styles, culture);
#endif

        return new(north, east, south, west);
    }

    public void Deconstruct(out double CenterLatitude, out double CenterLongitude)
    {
        CenterLatitude = (_West + _South) / 2;
        CenterLongitude = (_East + _North) / 2;
    }

    /// <summary>Распаковка области по географическим координатам границ</summary>
    /// <param name="North">Северная (верхняя) граница</param>
    /// <param name="East">Восточная (правая) граница</param>
    /// <param name="South">Южная (нижняя) граница</param>
    /// <param name="West">Западная (левая) граница</param>
    public void Deconstruct(out double North, out double East, out double South, out double West)
    {
        North = _North;
        East = _East;
        South = _South;
        West = _West;
    }

    public override string ToString()
    {
        var invariant_culture = CultureInfo.InvariantCulture;
        var result = new StringBuilderValued(stackalloc char[77])
           .Append(_North, invariant_culture).Append(',')
           .Append(_East, invariant_culture).Append(',')
           .Append(_South, invariant_culture).Append(',')
           .Append(_West, invariant_culture)
           .ToString();
        return result;

        //return new StringBuilder(90)
        //   .Append(_South.ToString(CultureInfo.InvariantCulture)).Append(',')
        //   .Append(_West.ToString(CultureInfo.InvariantCulture)).Append(',')
        //   .Append(_North.ToString(CultureInfo.InvariantCulture)).Append(',')
        //   .Append(_East.ToString(CultureInfo.InvariantCulture))
        //   .ToString();
    }

    public override int GetHashCode() => HashBuilder.Create().Append(_South).Append(_West).Append(_North).Append(_East);

    public bool Equals(BoundingBox? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _South.Equals(other._South)
            && _West.Equals(other._West)
            && _North.Equals(other._North)
            && _East.Equals(other._East);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BoundingBox)obj);
    }

    public static bool operator ==(BoundingBox left, BoundingBox right) => Equals(left, right);
    public static bool operator !=(BoundingBox left, BoundingBox right) => !Equals(left, right);

    public static BoundingBox operator *(BoundingBox Box, double Scale)
    {
        var (latitude, longitude) = Box;
        var (scaled_width_05, scaled_height_05) = (Scale * Box.Width / 2, Scale * Box.Height / 2);

        return new(latitude + scaled_height_05, longitude + scaled_width_05, latitude - scaled_height_05, longitude - scaled_width_05);
    }
}