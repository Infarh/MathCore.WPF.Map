using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Равноугольная проекция (X и Y соответствуют долготе и широте)</summary>
public class EquirectangularProjection : MapProjection
{
    public EquirectangularProjection() : this("EPSG:4326") { }

    public EquirectangularProjection(string CrsId) => this.CrsId = CrsId;

    public override Point GetMapScale(Location location) => new(
            x: ViewportScale / (MetersPerDegree * Math.Cos(location.Latitude * Consts.ToRad)),
            y: ViewportScale / MetersPerDegree);

    public override Point LocationToPoint(Location location) => new(location.Longitude, location.Latitude);

    public override Location PointToLocation(Point point) => new(point.Y, point.X);

    public override Location TranslateLocation(Location location, Point translation) => new(
             latitude: location.Latitude - translation.Y / ViewportScale,
            longitude: location.Longitude + translation.X / ViewportScale);
}