using System.Windows;

using MathCore.WPF.Map.Caching;

namespace MathCore.WPF.Map.TestWPF;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        //var query = new StringBuilderValued("GetMap")
        //   .Append("&LAYERS=").Append("Layers")
        //   .Append("&STYLES=").Append("Styles")
        //   .Append("&FORMAT=").Append("Format")
        //   .Append("&TRANSPARENT=").Append("FALSE")
        //   .Append('&').Append("qweqweqwe")
        //   .ToString();

        //var url = "https://core-renderer-tiles.maps.yandex.net"
        //    + "/vmap2/tiles?"
        //    + "lang=ru_RU&"
        //    + "x=2479&"
        //    + "y=1286&"
        //    + "z=12&"
        //    + "zmin=13"
        //    + "&zmax=13&"
        //    + "v=22.06.25-0-b220606200930&"
        //    + "ads=enabled&"
        //    + "experimental_dataset=near_home_market_poi&"
        //    + "experimental_data_poi=postprocess_poi_density_v4";

        //var bb = new BoundingBox(-11.0000000000054321, -123.000000000054321, -22.000000000054321, -132.000000000054321);

        //var loc = new Location(-11.0000000000054321, -123.000000000054321);

        //var loc_s = loc.ToString();

        //var bbs = bb.ToString();

        //Span<char> buffer = stackalloc char[10];
        //var sb = new StringBuilderValued(buffer);
        //sb.Append("val:");
        //sb.Append(-123.000000000054321);
        //var q_len = sb.Length;
        //var r_array = new char[q_len];
        //var q_span = r_array.AsSpan();
        //sb.TryCopyTo(q_span, out var copied);

        //var result = sb.ToString();

        base.OnStartup(e);

        TileImageLoader.Cache = new ImageFileCache("maps");
    }
}