using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Projections;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Источник данных тайлового слоя карты</summary>
[TypeConverter(typeof(TileSourceConverter))]
public class TileSource
{
    private static HttpClient GetClient()
    {
        var client = new HttpClient();

        var rnd = new Random();
        var user_agent = 
            $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            $"AppleWebKit/537.36 (KHTML, like Gecko) " +
            $"Chrome/124.0.0.0 " +
            $"YaBrowser/{rnd.Next(22, 26)}.{rnd.Next(0, 11)}.0.0 " +
            $"Safari/537.36";

        var headers = client.DefaultRequestHeaders;
        headers.Add("User-Agent", user_agent);
        headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        headers.Add("Accept-Language", "ru,en;q=0.9");
        headers.Add("Sec-Ch-Ua-Platform", "Windows");
        headers.Add("Sec-Gpc", "1");
        headers.Add("Upgrade-Insecure-Requests", "1");

        return client;
    }

    /// <summary>Клиент для выполнения запросов к тайловому серверу</summary>
    public static HttpClient HttpClient { get; set; } = GetClient();

    /// <summary>Проверка заголовков ответа на предмет наличия данных тайла, такого как <c>X-VE-Tile-Info = no-tile</c></summary>
    public static bool TileAvailable(HttpResponseHeaders ResponseHeaders) =>
        !ResponseHeaders.TryGetValues("X-VE-Tile-Info", out var tile_info) || !tile_info.Contains("no-tile");

    protected static Task<ImageSource> LoadLocalImageAsync(Uri Uri) => Task.Factory.StartNew(o =>
    {
        var uri = (Uri)o!;
        var path = uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString;

        if (!File.Exists(path)) return null;

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        return (ImageSource)BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    }, Uri)!;

    protected static async Task<ImageSource?> LoadHttpImageAsync(Uri uri)
    {
        using var response = await HttpClient.GetAsync(uri).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            Debug.WriteLine("TileSource: {0}: {1} {2}", uri, (int)response.StatusCode, response.ReasonPhrase);
        else if (TileAvailable(response.Headers))
        {
#if NET5_0_OR_GREATER
            await using var stream = new MemoryStream();
#else
            using var stream = new MemoryStream();
#endif
            await response.Content.CopyToAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }

        return null;
    }

    public TileSource() { }

    protected TileSource(string URIFormat) => _URIFormat = URIFormat;

    private Func<int, int, int, string?>? _GetUri;

    private int _SubdomainIndex = -1;

    public SubdomainsCollection? Subdomains { get; set; }

    private string? _URIFormat;

    /// <summary>Формат адреса данных тайлового сервера</summary>
    public string? UriFormat
    {
        get => _URIFormat;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("The value of the UriFormat property must not be null or empty.");

            _URIFormat = value;

            if (value.Contains("{x}") && value.Contains("{z}"))
                if (value.Contains("yandex"))
                    _GetUri = GetYandexUri;
                else if (value.Contains("{i}"))
                    _GetUri = GetIndexedUri;
                else if (value.Contains("{y}"))
                    _GetUri = GetDefaultUri;
                else if (value.Contains("{v}"))
                    _GetUri = GetTmsUri;
                else if (value.Contains("{q}")) // {i} is optional
                    _GetUri = GetQuadKeyUri;
                else if (value.Contains("{W}") && value.Contains("{S}") && value.Contains("{E}") && value.Contains("{N}"))
                    _GetUri = GetBoundingBoxUri;
                else if (value.Contains("{w}") && value.Contains("{s}") && value.Contains("{e}") && value.Contains("{n}"))
                    _GetUri = GetLatLonBoundingBoxUri;

            if (Subdomains is null && value.Contains("{c}"))
                Subdomains = new[] { "a", "b", "c" };
        }
    }

    /// <summary>Формирование адреса тайла на основе его координат</summary>
    public virtual Uri? GetUri(int x, int y, int ZoomLevel)
    {
        if (_GetUri is not { } get_uri || get_uri(x, y, ZoomLevel) is not { } uri)
            return null;

        if (Subdomains is not { Length: > 0 } || !uri.Contains("{c}"))
            return new(uri, UriKind.RelativeOrAbsolute);

        _SubdomainIndex = (_SubdomainIndex + 1) % Subdomains.Length;

        return new(uri.Replace("{c}", Subdomains[_SubdomainIndex]), UriKind.RelativeOrAbsolute);
    }

    /// <summary>Асинхронная загрузка изображения тайла по адресу, генерируемому GetUri(x, y, ZoomLevel)</summary>
    public virtual async Task<ImageSource?> LoadImageAsync(int x, int y, int ZoomLevel)
    {
        if (GetUri(x, y, ZoomLevel) is not { } uri)
            return null;

        try
        {
            return uri switch
            {
                { IsAbsoluteUri: false, Scheme: "file" } => await LoadLocalImageAsync(uri).ConfigureAwait(false),
                { Scheme: "http" } => await LoadHttpImageAsync(uri).ConfigureAwait(false),
                { Scheme: "https" } => await LoadHttpImageAsync(uri).ConfigureAwait(false),
                _ => new BitmapImage(uri)
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine("TileSource: {0}: {1}", uri, ex.Message);
            return null;
        }
    }

    private string GetYandexUri(int x, int y, int ZoomLevel) => _URIFormat!
       .Replace("{x}", x.ToString())
       .Replace("{y}", y.ToString())
       .Replace("{z}", ZoomLevel.ToString());

    private string GetDefaultUri(int x, int y, int ZoomLevel) => _URIFormat!
       .Replace("{x}", x.ToString())
       .Replace("{y}", y.ToString())
       .Replace("{z}", ZoomLevel.ToString());

    private string GetIndexedUri(int x, int y, int ZoomLevel) => _URIFormat!
       .Replace("{i}", "1")
       .Replace("{x}", x.ToString())
       .Replace("{y}", y.ToString())
       .Replace("{z}", ZoomLevel.ToString());

    private string GetTmsUri(int x, int y, int ZoomLevel) => _URIFormat!
       .Replace("{x}", x.ToString())
       .Replace("{v}", ((1 << ZoomLevel) - 1 - y).ToString())
       .Replace("{z}", ZoomLevel.ToString());

    private string? GetQuadKeyUri(int x, int y, int ZoomLevel)
    {
        if (ZoomLevel < 1)
            return null;

        var quad_key = new char[ZoomLevel];

        for (var z = ZoomLevel - 1; z >= 0; z--, x >>= 1, y >>= 1)
            quad_key[z] = (char)('0' + 2 * (y % 2) + x % 2);

        return _URIFormat!
           .Replace("{i}", new(quad_key, ZoomLevel - 1, 1))
           .Replace("{q}", new(quad_key));
    }

    private string GetBoundingBoxUri(int x, int y, int ZoomLevel)
    {
        var tile_size = 360d / (1 << ZoomLevel); // ширина тайла в градусах
        var west = MapProjection.MetersPerDegree * (x * tile_size - 180);
        var east = MapProjection.MetersPerDegree * ((x + 1) * tile_size - 180);
        var south = MapProjection.MetersPerDegree * (180 - (y + 1) * tile_size);
        var north = MapProjection.MetersPerDegree * (180 - y * tile_size);

        return _URIFormat!
           .Replace("{W}", west.ToString(CultureInfo.InvariantCulture))
           .Replace("{S}", south.ToString(CultureInfo.InvariantCulture))
           .Replace("{E}", east.ToString(CultureInfo.InvariantCulture))
           .Replace("{N}", north.ToString(CultureInfo.InvariantCulture))
           .Replace("{X}", MapProjection.TileSize.ToString())
           .Replace("{Y}", MapProjection.TileSize.ToString());
    }

    private string GetLatLonBoundingBoxUri(int x, int y, int ZoomLevel)
    {
        var tile_size = 360d / (1 << ZoomLevel); // ширина тайла в градусах
        var west = x * tile_size - 180;
        var east = (x + 1) * tile_size - 180;
        var south = WebMercatorProjection.YToLatitude(180 - (y + 1) * tile_size);
        var north = WebMercatorProjection.YToLatitude(180 - y * tile_size);

        return _URIFormat!
           .Replace("{w}", west.ToString(CultureInfo.InvariantCulture))
           .Replace("{s}", south.ToString(CultureInfo.InvariantCulture))
           .Replace("{e}", east.ToString(CultureInfo.InvariantCulture))
           .Replace("{n}", north.ToString(CultureInfo.InvariantCulture))
           .Replace("{X}", MapProjection.TileSize.ToString())
           .Replace("{Y}", MapProjection.TileSize.ToString());
    }
}