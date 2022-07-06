namespace MathCore.WPF.Map.TileLayers;

public class BingMapsTileSource : TileSource
{
    private readonly string[] _Subdomains;

    public BingMapsTileSource(string URIFormat, string[] subdomains) : base(URIFormat) => _Subdomains = subdomains;

    public override Uri GetUri(int x, int y, int ZoomLevel)
    {
        if (ZoomLevel < 1)
            return null;

        var subdomain = _Subdomains[(x + y) % _Subdomains.Length];
        var quad_key = new char[ZoomLevel];

        for (var z = ZoomLevel - 1; z >= 0; z--, x /= 2, y /= 2)
            quad_key[z] = (char)('0' + 2 * (y % 2) + x % 2);

        return new Uri(UriFormat
           .Replace("{subdomain}", subdomain)
           .Replace("{quadkey}", new string(quad_key)));
    }
}