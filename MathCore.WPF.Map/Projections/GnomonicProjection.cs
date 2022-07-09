using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Проекция координат гео-сервера</summary>
public class GnomonicProjection : AzimuthalProjection
{
    public GnomonicProjection() : this("AUTO2:97001") { }

    public GnomonicProjection(string CrsId) => this.CrsId = CrsId;

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