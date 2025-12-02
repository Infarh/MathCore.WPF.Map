using System.Windows;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Проекция (world) Меркатора</summary>
/// <remarks>
/// Долгота преобразуется линейно в значчения координаты X в метрах путём умножения на константу MetersPerDegree.
/// Широта в интервале [-MaxLatitude .. MaxLatitude] преобразуется в значения координаты Y в метрах
/// в интервале [-R*pi .. R*pi], где R = экваториальный радиус Земли.
/// </remarks>
public class WorldMercatorProjection : MapProjection
{
    public WorldMercatorProjection() : this("EPSG:3395") { }

    public WorldMercatorProjection(string CrsId)
    {
        this.CrsId = CrsId;
        IsWebMercator = true;
        LongitudeScale = MetersPerDegree;
        MaxLatitude = YToLatitude(180d);
    }

    public override double GetViewportScale(double ZoomLevel) => DegreesToViewportScale(ZoomLevel) / MetersPerDegree;

    public override Point GetMapScale(Location location)
    {
        var scale = ViewportScale / Math.Cos(location.Latitude * Consts.ToRad);

        return new(scale, scale);
    }

    public override Point LocationToPoint(Location location) => new(
            MetersPerDegree * location.Longitude,
            MetersPerDegree * LatitudeToY(location.Latitude));

    public override Location PointToLocation(Point point) => new(
         latitude: YToLatitude(point.Y / MetersPerDegree),
        longitude: point.X / MetersPerDegree);

    public override Location TranslateLocation(Location location, Point translation)
    {
        var scale_x = MetersPerDegree * ViewportScale;
        var (lat, lon) = location;
        var scale_y = scale_x / Math.Cos(lat * Consts.ToRad);

        return new(
             latitude: lat - (translation.Y / scale_y),
            longitude: lon + (translation.X / scale_x));
    }

    public static double LatitudeToY(double latitude)
    {
        switch (latitude)
        {
            case <= -90d: return double.NegativeInfinity;
            case >= +90d: return double.PositiveInfinity;
            default:
                var lat = latitude * Consts.ToRad;
                return Math.Log(Math.Tan((0.5 * lat) + Consts.PI025) * ConformalFactor(lat)) * Consts.ToDeg; // p.44 (7-7)
        }
    }

    public static double YToLatitude(double y)
    {
        var e = Math.Exp(-y * Consts.ToRad); // p.44 (7-10)
        var lat = Consts.PI05 - (2 * Math.Atan(e)); // начальное приближение p.44 (7-11)

        const double accuracy = 1e-6; // требуемая точность в радианах
        const int max_iterations = 10; // защитный предел итераций

        for (var i = 0; i < max_iterations; i++)
        {
            var new_lat = Consts.PI05 - (2 * Math.Atan(e * ConformalFactor(lat))); // p.44 (7-9)
            var delta = Math.Abs(new_lat - lat); // абсолютная разница, более устойчивая метрика

            lat = new_lat; // обновляем приближение

            if (double.IsNaN(lat) || double.IsInfinity(lat)) // защититься от некорректных значений
                break;

            if (delta <= accuracy) // проверка сходимости
                break;
        }

        return lat * Consts.ToDeg;
    }

    private static double ConformalFactor(double lat)
    {
        var sin_lat = Eccentricity * Math.Sin(lat);
        return Math.Pow((1d - sin_lat) / (1d + sin_lat), Eccentricity / 2d);
    }
}