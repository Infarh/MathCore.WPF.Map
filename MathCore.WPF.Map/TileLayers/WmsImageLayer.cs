using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Слой WMS, загружающий растровые карты по стандартному интерфейсу Web Map Service</summary>
public class WmsImageLayer : MapImageLayer
{
    #region Property ServerUri : Uri

    public static readonly DependencyProperty ServerUriProperty = DependencyProperty
       .Register(
            nameof(ServerUri),
            typeof(Uri),
            typeof(WmsImageLayer),
            new(null, (o, _) => ((WmsImageLayer)o).UpdateImage()));

    /// <summary>Базовый адрес WMS‑сервера</summary>
    public Uri? ServerUri
    {
        get => (Uri?)GetValue(ServerUriProperty);
        set => SetValue(ServerUriProperty, value);
    }

    #endregion

    #region Property Version : string

    public static readonly DependencyProperty VersionProperty = DependencyProperty
      .Register(
           nameof(Version),
           typeof(string),
           typeof(WmsImageLayer),
           new("1.3.0", (o, _) => ((WmsImageLayer)o).UpdateImage()));

    /// <summary>Версия протокола WMS</summary>
    public string Version
    {
        get => (string)GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    #endregion

    #region Property Layers : string

    public static readonly DependencyProperty LayersProperty = DependencyProperty
       .Register(
            nameof(Layers),
            typeof(string),
            typeof(WmsImageLayer),
            new(string.Empty, (o, _) => ((WmsImageLayer)o).UpdateImage()));

    /// <summary>Список слоёв WMS через запятую</summary>
    public string Layers
    {
        get => (string)GetValue(LayersProperty);
        set => SetValue(LayersProperty, value);
    }

    #endregion

    #region Property Styles : string

    public static readonly DependencyProperty StylesProperty = DependencyProperty
       .Register(
            nameof(Styles),
            typeof(string),
            typeof(WmsImageLayer),
            new(string.Empty, (o, _) => ((WmsImageLayer)o).UpdateImage()));

    /// <summary>Стиль отрисовки слоёв</summary>
    public string Styles
    {
        get => (string)GetValue(StylesProperty);
        set => SetValue(StylesProperty, value);
    }

    #endregion

    #region Property Format : string

    public static readonly DependencyProperty FormatProperty = DependencyProperty
       .Register(
            nameof(Format),
            typeof(string),
            typeof(WmsImageLayer),
            new("image/png", (o, _) => ((WmsImageLayer)o).UpdateImage()));

    /// <summary>Формат изображения, возвращаемого сервером</summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    #endregion

    #region Property Transparent : bool

    public static readonly DependencyProperty TransparentProperty = DependencyProperty
       .Register(
            nameof(Transparent),
            typeof(bool),
            typeof(WmsImageLayer),
            new(false, (o, _) => ((WmsImageLayer)o).UpdateImage()));

    /// <summary>Включить прозрачность изображения</summary>
    public bool Transparent
    {
        get => (bool)GetValue(TransparentProperty);
        set => SetValue(TransparentProperty, value);
    }

    #endregion

    private string _Layers = string.Empty; // локальная копия имени слоёв

    /// <summary>Создаёт изображение для заданного прямоугольника карты</summary>
    /// <param name="BoundingBox">Границы области</param>
    /// <returns>Источник изображения WMS</returns>
    protected override ImageSource? GetImage(BoundingBox BoundingBox)
    {
        if (ServerUri is null)
            return null;

        var projection_parameters = ParentMap?.LayerMapProjection.WmsQueryParameters(BoundingBox, Version);

        if (string.IsNullOrEmpty(projection_parameters))
            return null;

        var query = new StringBuilderValued(100)
           .Append("GetMap")
           .Append("&LAYERS=").Append(Layers)
           .Append("&STYLES=").Append(Styles)
           .Append("&FORMAT=").Append(Format)
           .Append("&TRANSPARENT=").Append(Transparent ? "TRUE" : "FALSE")
           .Append('&').Append(projection_parameters);

        var uri = GetRequestUri(query);
        return new BitmapImage(uri);
    }

    /// <summary>Запрашивает доступные имена слоёв на сервере</summary>
    /// <returns>Список имён слоёв или null при ошибке</returns>
    public async Task<IList<string>?> GetLayerNamesAsync()
    {
        if (ServerUri is null)
            return null;

        var layer_names = new List<string>();

        try
        {
            var xml = await Task.Factory.StartNew(v => XDocument.Load((string)v!), GetRequestUri("GetCapabilities").ToString()).ConfigureAwait(false);
            layer_names.AddRange(xml.XPathSelectElements("//Name").Select(node => node.Value));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WmsImageLayer: {0}: {1}", ServerUri, ex.Message);
        }

        return layer_names;
    }

    private Uri GetRequestUri(string query)
    {
        var address = ServerUri!.ToString();
        var uri = new StringBuilder(512).Append(address);

        if (uri[^1] is not ('?' or '&'))
            uri.Append('?');

        uri.Append("SERVICE=").Append("WMS").Append('&')
           .Append("VERSION=").Append(Version).Append('&')
           .Append("REQUEST=").Append(query)
           .Replace(" ", "%20");

        return new(uri.ToString());
    }

    private static IEnumerable<XmlElement> ChildElements(XmlElement element, string name) => element.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == name);
}