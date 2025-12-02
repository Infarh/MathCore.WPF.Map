using System.Diagnostics;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Источник тайлов, формируемых функцией</summary>
public sealed class FunctionalTileSource : TileSource
{
    /// <summary>Функция генерации изображения тайла</summary>
    /// <remarks>Диапазоны широты и долготы в градусах, размер тайла в пикселях</remarks>
    public Func<(double LatMin, double LatMax), (double LonMin, double LonMax), int, CancellationToken, Task<ImageSource?>>? TileFunc { get; set; }

    /// <summary>Конструктор по умолчанию для использования из XAML</summary>
    public FunctionalTileSource() { }

    /// <summary>Создаёт функциональный источник тайлов</summary>
    /// <param name="TileFunc">Функция генерации изображения тайла</param>
    public FunctionalTileSource(Func<(double LatMin, double LatMax), (double LonMin, double LonMax), int, CancellationToken, Task<ImageSource?>> TileFunc) => this.TileFunc = TileFunc;

    /// <summary>Асинхронная генерация изображения тайла</summary>
    public override async Task<ImageSource?> LoadImageAsync(int x, int y, int ZoomLevel)
    {
        var func = TileFunc;
        if (func is null) return null; // нет функции — нет тайла // кратко по делу

        // Вычисляем географические границы тайла для WebMercator
        var tile_size_deg = 360d / (1 << ZoomLevel);
        var lon_min = x * tile_size_deg - 180d;
        var lon_max = (x + 1) * tile_size_deg - 180d;
        var lat_min = WebMercatorProjection.YToLatitude(180 - (y + 1) * tile_size_deg);
        var lat_max = WebMercatorProjection.YToLatitude(180 - y * tile_size_deg);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // ограничение по времени
            return await func((lat_min, lat_max), (lon_min, lon_max), MapProjection.TileSize, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("FunctionalTileSource: z={0} x={1} y={2}: {3}", ZoomLevel, x, y, ex.Message);
            return null;
        }
    }
}
