namespace MathCore.WPF.Map.TileLayers;

public class BingMapsTileSource(string URIFormat, string[] subdomains) : TileSource(URIFormat)
{
    public override Uri? GetUri(int x, int y, int ZoomLevel)
    {
        if (ZoomLevel < 1)
            return null;

        var subdomain = subdomains[(x + y) % subdomains.Length];
        var quad_key = new char[ZoomLevel];

        for (var z = ZoomLevel - 1; z >= 0; z--, x /= 2, y /= 2)
            quad_key[z] = (char)('0' + 2 * (y % 2) + x % 2);

        return new(UriFormat!
           .Replace("{subdomain}", subdomain)
           .Replace("{quadkey}", new(quad_key)));
    }
}