using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Гномоническая азимутальная проекция</summary>
public class GnomonicProjection : AzimuthalProjection
{
    /// <summary>Создаёт проекцию с CRS по умолчанию AUTO2:97001</summary>
    public GnomonicProjection() : this("AUTO2:97001") { }

    /// <summary>Создаёт проекцию с указанным CRS</summary>
    /// <param name="CrsId">Идентификатор системы координат</param>
    public GnomonicProjection(string CrsId) => this.CrsId = CrsId;

    /// <summary>Преобразует географическую позицию в декартовые координаты проекции</summary>
    /// <param name="location">Географическая позиция</param>
    /// <returns>Точка в системе координат проекции</returns>
    public override Point LocationToPoint(Location location)
    {
        if (location.Equals(ProjectionCenter))
            return new();

        var (azimuth, distance) = GetAzimuthDistance(ProjectionCenter, location);

        var map_distance = distance < Consts.PI05
            ? Wgs84EquatorialRadius * Math.Tan(distance)
            : double.PositiveInfinity;

        return new(map_distance * Math.Sin(azimuth), map_distance * Math.Cos(azimuth));
    }

    /// <summary>Преобразует декартовые координаты проекции в географическую позицию</summary>
    /// <param name="point">Точка в системе координат проекции</param>
    /// <returns>Географическая позиция</returns>
    public override Location PointToLocation(Point point)
    {
        if (point is { X: 0, Y: 0 })
            return ProjectionCenter;

        var (x, y) = point;

        var azimuth = Math.Atan2(x, y);
        var map_distance = Math.Sqrt(x * x + y * y);
        var distance = Math.Atan2(map_distance, Wgs84EquatorialRadius);

        return GetLocation(ProjectionCenter, azimuth, distance);
    }
}