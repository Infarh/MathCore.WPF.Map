using System.Windows;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Азимутальная эквидистантная проекция</summary>
public class AzimuthalEquidistantProjection : AzimuthalProjection
{
    public AzimuthalEquidistantProjection() { }

    public AzimuthalEquidistantProjection(string CrsId) => this.CrsId = CrsId;

    public override Point LocationToPoint(Location location)
    {
        if (location.Equals(ProjectionCenter))
            return new();

        var (azimuth, distance) = GetAzimuthDistance(ProjectionCenter, location);

        distance *= Wgs84EquatorialRadius;

        return new(distance * Math.Sin(azimuth), distance * Math.Cos(azimuth));
    }

    public override Location PointToLocation(Point point)
    {
        if (point is { X: 0, Y: 0 })
            return ProjectionCenter;

        var azimuth = Math.Atan2(point.X, point.Y);
        var distance = Math.Sqrt(point.X * point.X + point.Y * point.Y) / Wgs84EquatorialRadius;

        return GetLocation(ProjectionCenter, azimuth, distance);
    }
}