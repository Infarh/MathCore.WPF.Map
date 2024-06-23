using MathCore.WPF.Map.Commands.Base;
using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map.Commands;

public class ZoomToBoundsCommand(MapBase Map) : Command
{
    protected override bool CanExecute(object? p)
    {
        switch (p)
        {
            case Location[] { Length: < 2 }:
                return false;

            case Location[] locations:
                {
                    var p1 = locations[0];
                    for (var i = 1; i < locations.Length; i++)
                    {
                        var p2 = locations[i];
                        if (p1 != p2) return true;
                        p1 = p2;
                    }

                    return false;
                }

            case List<Location> { Count: < 2 }:
                return false;

            case List<Location> locations:
                {
                    var p1 = locations[0];
                    for (var i = 1; i < locations.Count; i++)
                    {
                        var p2 = locations[i];
                        if (p1 != p2) return true;
                        p1 = p2;
                    }

                    return false;
                }

            case ICollection<Location> locations:
                {
                    using var enumerator = locations.GetEnumerator();
                    if (!enumerator.MoveNext()) return false;
                    var p1 = enumerator.Current;

                    if (!enumerator.MoveNext()) return false;
                    var p2 = enumerator.Current;

                    while (p1 == p2)
                    {
                        if (!enumerator.MoveNext()) return false;
                        p1 = p2;
                        p2 = enumerator.Current;
                    }

                    return true;
                }

            case IEnumerable<Location>:
                return true;

            default:
                return false;
        }
    }

    protected override void Execute(object? p)
    {
        if(p is not IEnumerable<Location> locations) return;

        var lat_min = double.PositiveInfinity;
        var lon_min = double.PositiveInfinity;

        var lat_max = double.NegativeInfinity;
        var lon_max = double.NegativeInfinity;

        foreach (var (lat, lon) in locations)
        {
            lat_min = Math.Min(lat_min, lat);
            lon_min = Math.Min(lon_min, lon);

            lat_max = Math.Max(lat_max, lat);
            lon_max = Math.Max(lon_max, lon);
        }

        if(double.IsInfinity(lat_min)) return;

        Map.ZoomToBounds(new(lat_max, lat_min, lon_min, lon_max));
    }
}
