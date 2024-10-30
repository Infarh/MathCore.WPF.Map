using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

using MathCore.WPF.Map.Infrastructure;

using Microsoft.Extensions.Primitives;

namespace MathCore.WPF.Map.Primitives.Base;

/// <summary>Географическая точка на поверхности сферы, заданная своей широтой и долготой в градусах</summary>
[Serializable]
[TypeConverter(typeof(LocationConverter))]
[DebuggerDisplay("lat:{_Latitude}, lon:{_Longitude}")]
public sealed class Location : IEquatable<Location?>, IFormattable
{
    /// <summary>Широта</summary>
    private double _Latitude;

    /// <summary>Широта</summary>
    [JsonPropertyName("lat")]
    public double Latitude
    {
        get => _Latitude;
        set => _Latitude = Math.Min(Math.Max(value, -90d), 90d);
    }

    /// <summary>Долгота</summary>
    private double _Longitude;

    /// <summary>Долгота</summary>
    [JsonPropertyName("lon")]
    public double Longitude
    {
        get => _Longitude;
        set => _Longitude = value;
    }

    /// <summary>Новая географическая точка</summary>
    public Location() { }

    /// <summary>Географическая точка</summary>
    /// <param name="latitude">Широта (вертикаль)</param>
    /// <param name="longitude">Долгота (горизонталь)</param>
    public Location(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

#if DEBUG
    public bool Equals(Location? location)
    {
        if (location is not { _Latitude: var latitude, _Longitude: var longitude })
            return false;

        var lat_delta = Math.Abs(latitude - _Latitude);
        var lon_delta = Math.Abs(longitude - _Longitude);
        return lat_delta < 2e-9 && lon_delta < 2e-9;
    }
#else
    public bool Equals(Location? location) =>
        location is { _Latitude: var latitude, _Longitude: var longitude }
        && Math.Abs(latitude - _Latitude) < 1e-9
        && Math.Abs(longitude - _Longitude) < 1e-9;
#endif

    public override bool Equals(object? obj) => Equals(obj as Location);

    public static bool operator ==(Location? a, Location? b) => Equals(a, b);

    public static bool operator !=(Location? a, Location? b) => !(a == b);

    public override int GetHashCode() =>
#if NET5_0_OR_GREATER
        HashCode.Combine(_Latitude, _Longitude);
#else
        HashBuilder.Create().Append(_Latitude).Append(_Longitude);
#endif

    //private static readonly string __NumberFormat = "F5";

    public override string ToString()
    {
        var result = new StringBuilder();

        result.Append(_Latitude >= 0 ? 'N' : 'S');
        result.Append(_Latitude).Append('\u00b0');
        result.Append(", ");

        result.Append(_Longitude >= 0 ? 'E' : 'W');
        result.Append(_Longitude).Append('\u00b0');

        return result.ToString();
    }

    private static readonly char[] __Separators = [','];
    public static Location Parse(string s)
    {
        const NumberStyles number_styles = NumberStyles.Float;
        var invariant_culture = CultureInfo.InvariantCulture;

        var tokens = new StringSegment(s).Split(__Separators);

        using var token = tokens.GetEnumerator();
        if (!token.MoveNext())
            throw new FormatException("Location string must be a comma-separated pair of double values");
#if NET5_0_OR_GREATER
        var latitude = double.Parse(token.Current, number_styles, invariant_culture);
#else
        var latitude = double.Parse(token.Current.ToString(), number_styles, invariant_culture);
#endif

        if (!token.MoveNext())
            throw new FormatException("Location string must be a comma-separated pair of double values");
#if NET5_0_OR_GREATER
        var longitude = double.Parse(token.Current, number_styles, invariant_culture);
#else
        var longitude = double.Parse(token.Current.ToString(), number_styles, invariant_culture);
#endif

        return new(latitude, longitude);
    }

    public static Location Parse(StringSegment s)
    {
        const NumberStyles number_styles = NumberStyles.Float;
        var invariant_culture = CultureInfo.InvariantCulture;

        var tokens = s.Split(__Separators);

        using var token = tokens.GetEnumerator();
        if (!token.MoveNext())
            throw new FormatException("Location string must be a comma-separated pair of double values");
#if NET5_0_OR_GREATER
        var latitude = double.Parse(token.Current, number_styles, invariant_culture);
#else
        var latitude = double.Parse(token.Current.ToString(), number_styles, invariant_culture);
#endif

        if (!token.MoveNext())
            throw new FormatException("Location string must be a comma-separated pair of double values");
#if NET5_0_OR_GREATER
        var longitude = double.Parse(token.Current, number_styles, invariant_culture);
#else
        var longitude = double.Parse(token.Current.ToString(), number_styles, invariant_culture);
#endif

        return new(latitude, longitude);
    }

    /// <summary>Нормализованное значение долготы [-180..180]</summary>
    public static double NormalizeLongitude(double longitude) => longitude switch
    {
        < -180d => (longitude + 180d) % 360d + 180d,
        > 180d => (longitude - 180d) % 360d - 180d,
        _ => longitude
    };

    /// <summary>Ближайшее значение долготы</summary>
    /// <param name="longitude">Значение долготы</param>
    /// <param name="ReferenceLongitude">Опорное значение долготы</param>
    /// <returns></returns>
    internal static double NearestLongitude(double longitude, double ReferenceLongitude)
    {
        longitude = NormalizeLongitude(longitude);

        if (longitude > ReferenceLongitude + 180d)
            return longitude - 360d;

        if (longitude < ReferenceLongitude - 180d)
            return longitude + 360d;

        return longitude;
    }

    public void Deconstruct(out double Latitude, out double Longitude) => (Latitude, Longitude) = (_Latitude, _Longitude);

    public string ToString(string? format)
    {
        var result = new StringBuilder();

        result.Append(_Latitude >= 0 ? 'N' : 'S');
        result.Append(_Latitude.ToString(format)).Append('\u00b0');
        result.Append(", ");

        result.Append(_Longitude >= 0 ? 'E' : 'W');
        result.Append(_Longitude.ToString(format)).Append('\u00b0');

        return result.ToString();
    }

    public string ToString(string? format, IFormatProvider? provider)
    {
        var result = new StringBuilder();

        result.Append(_Latitude >= 0 ? 'N' : 'S');
        result.Append(_Latitude.ToString(format, provider)).Append('\u00b0');
        result.Append(", ");

        result.Append(_Longitude >= 0 ? 'E' : 'W');
        result.Append(_Longitude.ToString(format, provider)).Append('\u00b0');

        return result.ToString();
    }
}