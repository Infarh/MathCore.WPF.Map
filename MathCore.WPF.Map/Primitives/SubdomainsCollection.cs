using System.ComponentModel;

using Microsoft.Extensions.Primitives;

namespace MathCore.WPF.Map.Primitives;

[Serializable]
[TypeConverter(typeof(SubdomainsCollectionConverter))]
public class SubdomainsCollection(string[] Subdomains)
{
    private readonly string[] _Subdomains = Subdomains;

    public int Length => _Subdomains.Length;

    public string this[int Index] => _Subdomains[Index];

    private static readonly char[] __Separator = [',', ';', ' '];

    public static SubdomainsCollection Parse(string Str)
    {
        var str = new StringSegment(Str).Split(__Separator);
        var domains = new List<string>();

        foreach (var value in str)
            if (value is { HasValue: true, Length: > 0, Value: var v })
                domains.Add(v);

        return new([.. domains]);
    }

    public static implicit operator SubdomainsCollection(string[] Subdomains) => new(Subdomains);
}
