using System.Windows;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Стереографическая проекция</summary>
public class StereographicProjection : AzimuthalProjection
{
    public StereographicProjection() : this("AUTO2:97002") { }

    public StereographicProjection(string CrsId) => this.CrsId = CrsId;

    public override Point LocationToPoint(Location location)
    {
        var projection_center = ProjectionCenter;
        if (location.Equals(projection_center))
            return new();

        var (azimuth, distance) = GetAzimuthDistance(ProjectionCenter, location);

        var map_distance = 2 * Wgs84EquatorialRadius * Math.Tan(distance / 2);

        return new(map_distance * Math.Sin(azimuth), map_distance * Math.Cos(azimuth));
    }

    public override Location PointToLocation(Point point)
    {
        var projection_center = ProjectionCenter;
        if (point is { X: 0, Y: 0 })
            return projection_center;

        var azimuth = Math.Atan2(point.X, point.Y);
        var map_distance = Math.Sqrt(point.X * point.X + point.Y * point.Y);
        var distance = 2d * Math.Atan(map_distance / (2d * Wgs84EquatorialRadius));

        return GetLocation(projection_center, azimuth, distance);
    }
}