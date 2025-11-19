using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Xml.Linq;

using MathCore.WPF.Map.Infrastructure;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Слой карты Bing Maps</summary>
public class BingMapsTileLayer : MapTileLayer
{
    public enum MapMode
    {
        Road, Aerial, AerialWithLabels
    }

    public BingMapsTileLayer() : this(new TileImageLoader()) { }

    public BingMapsTileLayer(ITileImageLoader TileImageLoader)
        : base(TileImageLoader)
    {
        MinZoomLevel = 1;
        MaxZoomLevel = 21;
        Loaded += OnLoaded;
    }

    /// <summary>Api-ключ доступа к сервису карты</summary>
    public static string? ApiKey { get; set; }

    public MapMode Mode { get; set; }

    public string? Culture { get; set; }

    public Uri? LogoImageUri { get; private set; }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (ApiKey is not { Length: > 0 and var api_key_length } api_key)
        {
            Debug.WriteLine("BingMapsTileLayer требует указания значения API-ключа");
            return;
        }

        const string uri_address = "http://dev.virtualearth.net/REST/V1/Imagery/Metadata/";
        const int buffer_capacity =
            53      // uri_address.Length
            + 16    // Mode max length
            + 16;   // "?output=xml&key=".Length
        var imagery_metadata_url_sb = new StringBuilderValued(buffer_capacity + api_key_length, uri_address)
           .Append(Mode)
           .Append("?output=xml&key=")
           .Append(api_key)
           .ToString();

        try
        {
            var xml = await Task.Factory.StartNew(v => XDocument.Load((string)v!), imagery_metadata_url_sb).ConfigureAwait(true);
            if (xml.Element("ImageryMetadata") is { } imagery_metadata2)
                ReadImageryMetadata(imagery_metadata2);

            if (xml.Element("BrandLogoUri") is { Value: { Length: > 0 } brand_logo_uri_text })
                LogoImageUri = new(brand_logo_uri_text);

            //var uri = new Uri(imagery_metadata_url + "?output=xml&key=" + ApiKey);
            //var uri = new Uri(imagery_metadata_url_sb);
            //var document = await XmlDocument.LoadFromUriAsync(uri).ConfigureAwait(true);

            //var imagery_metadata = document.DocumentElement!
            //   .GetElementsByTagName("ImageryMetadata")
            //   .OfType<XmlElement>()
            //   .FirstOrDefault();

            //if (imagery_metadata is not null)
            //    ReadImageryMetadata(imagery_metadata);

            //var brand_logo_uri = document.DocumentElement
            //   .GetElementsByTagName("BrandLogoUri")
            //   .OfType<XmlElement>()
            //   .FirstOrDefault();

            //if (brand_logo_uri is not null)
            //    LogoImageUri = new Uri(brand_logo_uri.InnerText);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("BingMapsTileLayer: {0}{1}: {2}", uri_address, Mode, ex.Message);
        }
    }

    //private void ReadImageryMetadata(XmlElement ImageryMetadata)
    //{
    //    string? image_url = null;
    //    string[]? image_url_subdomains = null;
    //    int? zoom_min = null;
    //    int? zoom_max = null;

    //    foreach (var element in ImageryMetadata.ChildNodes.OfType<XmlElement>())
    //        switch (element.LocalName)
    //        {
    //            case "ImageUrl":
    //                image_url = element.InnerText;
    //                break;
    //            case "ImageUrlSubdomains":
    //                image_url_subdomains = element.ChildNodes
    //                   .OfType<XmlElement>()
    //                   .Where(e => e.LocalName == "string")
    //                   .Select(e => e.InnerText)
    //                   .ToArray();
    //                break;
    //            case "ZoomMin":
    //                zoom_min = int.Parse(element.InnerText);
    //                break;
    //            case "ZoomMax":
    //                zoom_max = int.Parse(element.InnerText);
    //                break;
    //        }

    //    if (image_url is not { Length: > 0 } || image_url_subdomains is not { Length: > 0 }) return;

    //    if (zoom_min.HasValue && zoom_min.Value > MinZoomLevel)
    //        MinZoomLevel = zoom_min.Value;

    //    if (zoom_max.HasValue && zoom_max.Value < MaxZoomLevel)
    //        MaxZoomLevel = zoom_max.Value;

    //    if (string.IsNullOrEmpty(Culture))
    //        Culture = CultureInfo.CurrentUICulture.Name;

    //    TileSource = new BingMapsTileSource(image_url.Replace("{culture}", Culture), image_url_subdomains);
    //}

    private void ReadImageryMetadata(XElement ImageryMetadata)
    {
        string? image_url = null;
        string[]? image_url_subdomains = null;
        int? zoom_min = null;
        int? zoom_max = null;

        foreach (var element in ImageryMetadata.Elements())
            switch (element.Name.LocalName)
            {
                case "ImageUrl":
                    image_url = element.Value;
                    break;
                case "ImageUrlSubdomains":
                    image_url_subdomains = element.Elements("string").Select(n => n.Value).ToArray();
                    break;
                case "ZoomMin":
                    zoom_min = (int)element;
                    break;
                case "ZoomMax":
                    zoom_max = (int)element;
                    break;
            }

        if (image_url is not { Length: > 0 } || image_url_subdomains is not { Length: > 0 }) return;

        if (zoom_min.HasValue && zoom_min.Value > MinZoomLevel)
            MinZoomLevel = zoom_min.Value;

        if (zoom_max.HasValue && zoom_max.Value < MaxZoomLevel)
            MaxZoomLevel = zoom_max.Value;

        if (string.IsNullOrEmpty(Culture))
            Culture = CultureInfo.CurrentUICulture.Name;

        TileSource = new BingMapsTileSource(image_url.Replace("{culture}", Culture), image_url_subdomains);
    }
}