using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

public class YandexProjection : MapProjection
{
    public YandexProjection() : this("Yandex") { }

    public YandexProjection(string CrsId)
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

    public override Point LocationToPoint(Location location) => WGS84ToBing(location);

    public static Point WGS84ToBing(Location coordinate)
    {
        var lon = coordinate.Longitude * Consts.ToRad;
        var lat = coordinate.Latitude * Consts.ToRad;
        const double e = 0.0818191908426;
        var f = e * Math.Sin(lat);
        var h = Math.Tan(Consts.PI025 + lat / 2);
        var j = Math.Pow(Math.Tan(Consts.PI025 + Math.Asin(f) / 2), e);
        var i = h / j;

        const double R = 6378137;
        return new(R * lon, R * Math.Log(i));
    }

    public override Location PointToLocation(Point point) => BingtoWGS84Mercator(point);

    public static Location BingtoWGS84Mercator(Point point)
    {
        const double k = 180 / 20037508.34;
        var lat = point.Y * k;
        var lon = point.X * k;

        lat = 2 * Consts.ToDeg * Math.Atan(Math.Exp(lat * Consts.ToRad)) - 90;

        return new(lat, lon);
    }

    public override Location TranslateLocation(Location location, Point translation)
    {
        var scale_x = MetersPerDegree * ViewportScale;
        var scale_y = scale_x / Math.Cos(location.Latitude * Consts.ToRad);

        return new(
            latitude: location.Latitude - translation.Y / scale_y,
            longitude: location.Longitude + translation.X / scale_x);
    }

    public static double YToLatitude(double y)
    {
        var e = Math.Exp(-y * Consts.ToRad); // p.44 (7-10)
        var lat = Consts.PI05 - 2 * Math.Atan(e); // p.44 (7-11)

        const double accuracy = 1e-6;
        const int max_iterations = 10;
        for (var (i, delta)= (0, 1d); delta > accuracy && i < max_iterations; i++)
        {
            var new_lat = Consts.PI05 - 2 * Math.Atan(e * ConformalFactor(lat)); // p.44 (7-9)
            (lat, delta) = (new_lat, Math.Abs(1 - new_lat / lat));
        }

        static double ConformalFactor(double lat)
        {
            var sin_lat = Eccentricity * Math.Sin(lat);
            return Math.Pow((1d - sin_lat) / (1d + sin_lat), Eccentricity / 2d);
        }

        return lat * Consts.ToDeg;
    }
}
