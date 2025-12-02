using System.Windows;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

using static System.Math;
using static MathCore.WPF.Map.Infrastructure.Consts;

namespace MathCore.WPF.Map.Projections;

/// <summary>Проекция (веб) Меркатора</summary>
/// <remarks>
/// Долгота преобразуется линейно в значчения координаты X в метрах путём умножения на константу MetersPerDegree.
/// Широта в интервале [-MaxLatitude .. MaxLatitude] преобразуется в значения координаты Y в метрах
/// в интервале [-R*pi .. R*pi], где R = экваториальный радиус Земли.
/// </remarks>
public class WebMercatorProjection : MapProjection
{
    public WebMercatorProjection() : this("EPSG:3857") { }

    public WebMercatorProjection(string CrsId)
    {
        this.CrsId = CrsId;
        IsWebMercator = true;
        LongitudeScale = MetersPerDegree;
        MaxLatitude = YToLatitude(180d);
    }

    public override double GetViewportScale(double ZoomLevel) => DegreesToViewportScale(ZoomLevel) / MetersPerDegree;

    public override Point GetMapScale(Location location)
    {
        var scale = ViewportScale / Cos(location.Latitude * ToRad);

        return new(scale, scale);
    }

    public override Point LocationToPoint(Location location) => new(
        x: MetersPerDegree * location.Longitude,
        y: MetersPerDegree * LatitudeToY(location.Latitude));

    public override Location PointToLocation(Point point) => new(
         latitude: YToLatitude(point.Y / MetersPerDegree),
        longitude: point.X / MetersPerDegree);

    public override Location TranslateLocation(Location location, Point translation)
    {
        var scale_x = MetersPerDegree * ViewportScale;
        var (lat, lon) = location;
        var scale_y = scale_x / Cos(lat * ToRad);

        return new(
             latitude: lat - (translation.Y / scale_y),
            longitude: lon + (translation.X / scale_x));
    }

    public static double LatitudeToY(double latitude) => latitude switch
    {
        <= -90d => double.NegativeInfinity,
        >= +90d => double.PositiveInfinity,
        _ => Log(Tan((latitude + 90d) * 0.5 * ToRad)) * ToDeg
    };

    //public static double YToLatitude(double y) => 2 * Math.Atan(Math.Exp(y * Consts.ToRad)) * Consts.ToDeg - 90d;
    public static double YToLatitude(double y) => 90 - (2 * Atan(Exp(-y * ToRad)) * ToDeg);
}