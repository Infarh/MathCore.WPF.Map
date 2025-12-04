using System.Windows;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Азимутальная эквидистантная проекция</summary>
public class AzimuthalEquidistantProjection : AzimuthalProjection
{
    /// <summary>Создаёт проекцию с параметрами по умолчанию</summary>
    public AzimuthalEquidistantProjection() { }

    /// <summary>Создаёт проекцию с указанным CRS</summary>
    /// <param name="CrsId">Идентификатор системы координат</param>
    public AzimuthalEquidistantProjection(string CrsId) => this.CrsId = CrsId;

    /// <summary>Преобразует географическую позицию в декартовые координаты проекции</summary>
    /// <param name="location">Географическая позиция</param>
    /// <returns>Точка в системе координат проекции</returns>
    public override Point LocationToPoint(Location location)
    {
        if (location.Equals(ProjectionCenter))
            return new();

        var (azimuth, distance) = GetAzimuthDistance(ProjectionCenter, location);

        distance *= Wgs84EquatorialRadius;

        return new(distance * Math.Sin(azimuth), distance * Math.Cos(azimuth));
    }

    /// <summary>Преобразует декартовые координаты проекции в географическую позицию</summary>
    /// <param name="point">Точка в системе координат проекции</param>
    /// <returns>Географическая позиция</returns>
    public override Location PointToLocation(Point point)
    {
        if (point is { X: 0, Y: 0 })
            return ProjectionCenter;

        var azimuth = Math.Atan2(point.X, point.Y);
        var distance = Math.Sqrt(point.X * point.X + point.Y * point.Y) / Wgs84EquatorialRadius;

        return GetLocation(ProjectionCenter, azimuth, distance);
    }
}