using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Ортогональная проекция</summary>
public class OrthographicProjection : AzimuthalProjection
{
    /// <summary>Инициализирует проекцию с CRS по умолчанию</summary>
    public OrthographicProjection() : this("AUTO2:42003") { }

    /// <summary>Инициализирует проекцию с указанным идентификатором CRS</summary>
    /// <param name="CrsId">Идентификатор системы координат</param>
    public OrthographicProjection(string CrsId) => this.CrsId = CrsId;

    /// <summary>Преобразует географические координаты в экранную точку</summary>
    /// <param name="location">Географическое положение</param>
    /// <returns>Точка в проекции</returns>
    public override Point LocationToPoint(Location location)
    {
        if (location.Equals(ProjectionCenter))
            return new();

        var lat0 = ProjectionCenter.Latitude * Consts.ToRad;
        var lat = location.Latitude * Consts.ToRad;
        var d_lon = (location.Longitude - ProjectionCenter.Longitude) * Consts.ToRad;

        return new(
            x: Wgs84EquatorialRadius * Math.Cos(lat) * Math.Sin(d_lon),
            y: Wgs84EquatorialRadius * (Math.Cos(lat0) * Math.Sin(lat) - Math.Sin(lat0) * Math.Cos(lat) * Math.Cos(d_lon)));
    }

    /// <summary>Преобразует экранную точку в географические координаты</summary>
    /// <param name="point">Точка проекции</param>
    /// <returns>Географическое положение</returns>
    public override Location PointToLocation(Point point)
    {
        if (point is { X: 0, Y: 0 })
            return ProjectionCenter;

        var x = point.X / Wgs84EquatorialRadius;
        var y = point.Y / Wgs84EquatorialRadius;
        var r2 = x * x + y * y;

        if (r2 > 1d)
            return new(double.NaN, double.NaN);

        var r = Math.Sqrt(r2);
        var sin_c = r;
        var cos_c = Math.Sqrt(1 - r2);

        var lat0 = ProjectionCenter.Latitude * Consts.ToRad;
        var cos_lat0 = Math.Cos(lat0);
        var sin_lat0 = Math.Sin(lat0);

        return new(
            latitude: Consts.ToDeg * Math.Asin(cos_c * sin_lat0 + y * sin_c * cos_lat0 / r),
            longitude: Consts.ToDeg * Math.Atan2(x * sin_c, r * cos_c * cos_lat0 - y * sin_c * sin_lat0) + ProjectionCenter.Longitude);
    }
}