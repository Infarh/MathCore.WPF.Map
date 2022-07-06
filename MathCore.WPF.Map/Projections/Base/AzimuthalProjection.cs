using System.Globalization;
using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map.Projections.Base;

/// <summary>Азимутальная проекция</summary>
public abstract class AzimuthalProjection : MapProjection
{
    public Location ProjectionCenter { get; private set; } = new();

    protected AzimuthalProjection()
    {
        IsAzimuthal = true;
        LongitudeScale = double.NaN;
    }

    public override double GetViewportScale(double ZoomLevel) => DegreesToViewportScale(ZoomLevel) / MetersPerDegree;

    public override Point GetMapScale(Location location) => new(ViewportScale, ViewportScale);

    public override Location TranslateLocation(Location location, Point translation)
    {
        var scale_y = ViewportScale * MetersPerDegree;
        var scale_x = scale_y * Math.Cos(location.Latitude * Consts.ToRad);

        return new(
            latitude: location.Latitude - translation.Y / scale_y,
            longitude: location.Longitude + translation.X / scale_x);
    }

    public override Rect BoundingBoxToRect(BoundingBox BoundingBox)
    {
        var center = LocationToPoint(BoundingBox.GetCenter());

        return new(
            x: center.X - BoundingBox.Width / 2d,
            y: center.Y - BoundingBox.Height / 2d,
            width: BoundingBox.Width,
            height: BoundingBox.Height);
    }

    public override BoundingBox RectToBoundingBox(Rect rect)
    {
        var center = PointToLocation(new Point(rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d));

        return new BoundingBox(center, rect.Width, rect.Height); // width и height в метрах
    }

    public override void SetViewportTransform(Location Center, Location MapCenter, Point ViewportCenter, double ZoomLevel, double Heading)
    {
        ProjectionCenter = Center;

        base.SetViewportTransform(Center, MapCenter, ViewportCenter, ZoomLevel, Heading);
    }

    public override string WmsQueryParameters(BoundingBox BoundingBox, string Version = "1.3.0")
    {
        if (string.IsNullOrEmpty(CrsId))
            return null;

        var rect = BoundingBoxToRect(BoundingBox);
        var width = (int)Math.Round(ViewportScale * rect.Width);
        var height = (int)Math.Round(ViewportScale * rect.Height);
        var crs = Version.StartsWith("1.1.") ? "SRS" : "CRS";

        return string.Format(CultureInfo.InvariantCulture,
            "{0}={1},1,{2},{3}&BBOX={4},{5},{6},{7}&WIDTH={8}&HEIGHT={9}",
            crs,
            CrsId,
            ProjectionCenter.Longitude,
            ProjectionCenter.Latitude,
            rect.X,
            rect.Y,
            rect.X + rect.Width,
            rect.Y + rect.Height,
            width,
            height);
    }

    /// <summary>Расчёт азимута и дистанции в радианах между location1 и location2</summary>
    public static (double Azimuth, double Distance) GetAzimuthDistance(Location location1, Location location2)
    {
        var lat1 = location1.Latitude * Consts.ToRad;
        var lon1 = location1.Longitude * Consts.ToRad;
        var lat2 = location2.Latitude * Consts.ToRad;
        var lon2 = location2.Longitude * Consts.ToRad;
        var cos_lat1 = Math.Cos(lat1);
        var sin_lat1 = Math.Sin(lat1);
        var cos_lat2 = Math.Cos(lat2);
        var sin_lat2 = Math.Sin(lat2);
        var cos_lon12 = Math.Cos(lon2 - lon1);
        var sin_lon12 = Math.Sin(lon2 - lon1);
        var cos_distance = sin_lat1 * sin_lat2 + cos_lat1 * cos_lat2 * cos_lon12;

        var azimuth = Math.Atan2(sin_lon12, cos_lat1 * sin_lat2 / cos_lat2 - sin_lat1 * cos_lon12);
        var distance = Math.Acos(Math.Max(Math.Min(cos_distance, 1d), -1d));

        return (azimuth, distance);
    }

    /// <summary>Расчёт координат относительно указанной точки, а также азимута и расстояния</summary>
    public static Location GetLocation(Location location, double azimuth, double distance)
    {
        var lat1 = location.Latitude * Consts.ToRad;
        var sin_distance = Math.Sin(distance);
        var cos_distance = Math.Cos(distance);
        var cos_azimuth = Math.Cos(azimuth);
        var sin_azimuth = Math.Sin(azimuth);
        var cos_lat1 = Math.Cos(lat1);
        var sin_lat1 = Math.Sin(lat1);
        var sin_lat2 = sin_lat1 * cos_distance + cos_lat1 * sin_distance * cos_azimuth;
        var lat2 = Math.Asin(Math.Max(Math.Min(sin_lat2, 1d), -1d));
        var d_lon = Math.Atan2(sin_distance * sin_azimuth, cos_lat1 * cos_distance - sin_lat1 * sin_distance * cos_azimuth);

        return new(lat2 * Consts.ToDeg, location.Longitude + d_lon * Consts.ToDeg);
    }
}