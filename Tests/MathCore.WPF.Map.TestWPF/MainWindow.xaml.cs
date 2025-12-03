using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.TestWPF;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        //Loaded += (_, _) => InitFunctionalLayer();
    }

    private void InitFunctionalLayer()
    {
        var center = new Location(55.75, 37.62);

        var func_source = new FunctionalTileSource
        {
            TileFunc = async (lat_range, lon_range, tile_size, ct) =>
            {
                var wb = new WriteableBitmap(tile_size, tile_size, 96, 96, PixelFormats.Bgra32, null);
                var stride = wb.BackBufferStride;
                var pixels = new byte[stride * tile_size];

                var lat_min = lat_range.LatMin;
                var lat_max = lat_range.LatMax;
                var lon_min = lon_range.LonMin;
                var lon_max = lon_range.LonMax;

                for (var y = 0; y < tile_size; y++)
                {
                    ct.ThrowIfCancellationRequested();
                    var lat = lat_max - ((lat_max - lat_min) * y / (tile_size - 1.0));
                    for (var x = 0; x < tile_size; x++)
                    {
                        var lon = lon_min + ((lon_max - lon_min) * x / (tile_size - 1.0));
                        var loc = new Location(lat, lon);

                        var distance_rad = Projections.Base.AzimuthalProjection.GetAzimuthDistance(center, loc).Distance; // радианы
                        var distance_m = distance_rad * Projections.Base.MapProjection.Wgs84EquatorialRadius; // м

                        var r = distance_m / 1000.0; // нормировка
                        var f = r == 0 ? 1.0 : Math.Sin(r) / r;
                        f = Math.Max(0, f);

                        var (b, g, r8) = HeatColor(f);
                        var index = (y * stride) + (x * 4);
                        pixels[index + 0] = b; // B
                        pixels[index + 1] = g; // G
                        pixels[index + 2] = r8; // R
                        pixels[index + 3] = 200; // A чуть прозрачный
                    }
                }

                wb.WritePixels(new Int32Rect(0, 0, tile_size, tile_size), pixels, stride, 0);
                return wb;
            }
        };

        var func_layer = new FunctionalTileLayer
        {
            SourceName = "FuncLayer",
            Description = "sin(x)/x heatmap",
            TileSource = func_source,
            MaxZoomLevel = 21,
            Opacity = 0.6
        };

        // Добавляем слой поверх текущей базовой карты
        MapControl.Children.Add(func_layer);
    }

    private static (byte B, byte G, byte R) HeatColor(double v)
    {
        v = Math.Max(0, Math.Min(1, v));
        double r, g, b;
        if (v < 0.25)
        {
            var t = v / 0.25;
            r = 0; g = t * 255; b = 255;
        }
        else if (v < 0.5)
        {
            var t = (v - 0.25) / 0.25;
            r = 0; g = 255; b = (1 - t) * 255;
        }
        else if (v < 0.75)
        {
            var t = (v - 0.5) / 0.25;
            r = t * 255; g = 255; b = 0;
        }
        else
        {
            var t = (v - 0.75) / 0.25;
            r = 255; g = (1 - t) * 255; b = 0;
        }
        return ((byte)b, (byte)g, (byte)r);
    }
}
