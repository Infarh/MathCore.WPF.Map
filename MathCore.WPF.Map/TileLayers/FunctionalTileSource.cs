using System.Diagnostics;
using System.Windows.Media;

using MathCore.WPF.Map.Projections;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Делегат генерации изображения тайла</summary>
/// <param name="LatitudeRange">Диапазон широт в градусах: минимальная и максимальная широта (LatMin, LatMax)</param>
/// <param name="LongitudeRange">Диапазон долгот в градусах: минимальная и максимальная долгота (LonMin, LonMax)</param>
/// <param name="TilePixelSize">Размер тайла в пикселях по обеим осям</param>
/// <param name="Cancellation">Токен отмены операции генерации тайла</param>
/// <returns>Асинхронный результат с изображением тайла или null, если тайл недоступен/операция отменена</returns>
public delegate Task<ImageSource?> FunctionalTileSourceDelegate(
    (double LatMin, double LatMax) LatitudeRange,
    (double LonMin, double LonMax) LongitudeRange,
    int TilePixelSize,
    CancellationToken Cancellation
);

/// <summary>Источник тайлов, формируемых функцией</summary>
public sealed class FunctionalTileSource : TileSource
{
    /// <summary>Функция генерации изображения тайла</summary>
    /// <remarks>Диапазоны широты и долготы в градусах, размер тайла в пикселях</remarks>
    public FunctionalTileSourceDelegate? TileFunc { get; set; }

    /// <summary>Конструктор по умолчанию для использования из XAML</summary>
    public FunctionalTileSource() { }

    /// <summary>Создаёт функциональный источник тайлов</summary>
    /// <param name="TileFunc">Функция генерации изображения тайла</param>
    public FunctionalTileSource(FunctionalTileSourceDelegate TileFunc) => this.TileFunc = TileFunc;

    /// <summary>Асинхронная генерация изображения тайла</summary>
    public override async Task<ImageSource?> LoadImageAsync(int x, int y, int ZoomLevel)
    {
        var func = TileFunc;
        if (func is null) return null; // нет функции — нет тайла // кратко по делу

        // Вычисляем географические границы тайла для WebMercator
        var tile_size_deg = 360d / (1 << ZoomLevel);
        var lon_min = (x * tile_size_deg) - 180d;
        var lon_max = ((x + 1) * tile_size_deg) - 180d;
        var lat_min = WebMercatorProjection.YToLatitude(180 - ((y + 1) * tile_size_deg));
        var lat_max = WebMercatorProjection.YToLatitude(180 - (y * tile_size_deg));

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
