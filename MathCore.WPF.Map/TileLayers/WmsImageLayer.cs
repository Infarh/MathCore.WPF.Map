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

public class WmsImageLayer : MapImageLayer
{
    #region Property ServerUri : Uri

    public static readonly DependencyProperty ServerUriProperty = DependencyProperty
       .Register(
            nameof(ServerUri),
            typeof(Uri),
            typeof(WmsImageLayer),
            new PropertyMetadata(null, (o, _) => ((WmsImageLayer)o).UpdateImage()));

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
           new PropertyMetadata("1.3.0", (o, _) => ((WmsImageLayer)o).UpdateImage()));

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
            new PropertyMetadata(string.Empty, (o, _) => ((WmsImageLayer)o).UpdateImage()));

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
            new PropertyMetadata(string.Empty, (o, _) => ((WmsImageLayer)o).UpdateImage()));

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
            new PropertyMetadata("image/png", (o, _) => ((WmsImageLayer)o).UpdateImage()));

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
            new PropertyMetadata(false, (o, _) => ((WmsImageLayer)o).UpdateImage()));

    public bool Transparent
    {
        get => (bool)GetValue(TransparentProperty);
        set => SetValue(TransparentProperty, value);
    }

    #endregion

    private string _Layers = string.Empty;

    protected override ImageSource GetImage(BoundingBox BoundingBox)
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

        //var uri = GetRequestUri("GetMap"
        //    + "&LAYERS=" + Layers + "&STYLES=" + Styles + "&FORMAT=" + Format
        //    + "&TRANSPARENT=" + (Transparent ? "TRUE" : "FALSE") + "&" + projection_parameters);

        var uri = GetRequestUri(query);
        return new BitmapImage(uri);
    }

    public async Task<IList<string>> GetLayerNamesAsync()
    {
        if (ServerUri is null)
            return null;

        var layer_names = new List<string>();

        try
        {
            var xml = await Task.Factory.StartNew(v => XDocument.Load((string)v), GetRequestUri("GetCapabilities").ToString());
            layer_names.AddRange(xml.XPathSelectElements("//Name").Select(node => node.Value));

            //var document = await XmlDocument.LoadFromUriAsync(GetRequestUri("GetCapabilities"));
            //if (ChildElements(document.DocumentElement, "Capability").FirstOrDefault() is { } capability)
            //    if (ChildElements(capability, "Layer").FirstOrDefault() is { } root_layer)
            //        foreach (var layer in ChildElements(root_layer, "Layer"))
            //            if (ChildElements(layer, "Name").FirstOrDefault() is { InnerText: var inner_text })
            //                layer_names.Add(inner_text);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WmsImageLayer: {0}: {1}", ServerUri, ex.Message);
        }

        return layer_names;
    }

    private Uri GetRequestUri(string query)
    {
        var address = ServerUri.ToString();
        var uri = new StringBuilder(512).Append(address);

        if (uri[^1] is not ('?' or '&'))
            uri.Append('?');

        uri.Append("SERVICE=").Append("WMS").Append('&')
           .Append("VERSION=").Append(Version).Append('&')
           .Append("REQUEST=").Append(query)
           .Replace(" ", "%20");

        return new Uri(uri.ToString());
    }

    private static IEnumerable<XmlElement> ChildElements(XmlElement element, string name) => element.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == name);
}