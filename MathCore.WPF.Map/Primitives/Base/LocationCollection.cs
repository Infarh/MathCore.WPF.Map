using System.Collections.ObjectModel;
using System.ComponentModel;

using Microsoft.Extensions.Primitives;

namespace MathCore.WPF.Map.Primitives.Base;

[TypeConverter(typeof(LocationCollectionConverter))]
public class LocationCollection : ObservableCollection<Location>
{
    public LocationCollection() { }

    public LocationCollection(IEnumerable<Location> locations) : base(locations) { }

    public LocationCollection(List<Location> locations) : base(locations) { }

    private static readonly char[] __Separators = { ' ', ';' };

    public static LocationCollection Parse(string Str) => new(ParseLocations(Str));

    private static IEnumerable<Location> ParseLocations(string Str)
    {
        foreach (var token in new StringSegment(Str).Split(__Separators))
            if (token.HasValue && token.Length > 0)
                yield return Location.Parse(token);
    }
}