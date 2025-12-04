using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;

using MathCore.WPF.Map.Commands;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.TileLayers;

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
        if (func is null) return null; // нет функции — нет тайла

        // Вычисляем географические границы тайла для WebMercator
        var tile_size_deg = 360d / (1 << ZoomLevel);
        var lon_min = (x * tile_size_deg) - 180d;
        var lon_max = ((x + 1) * tile_size_deg) - 180d;
        var lat_min = WebMercatorProjection.YToLatitude(180 - ((y + 1) * tile_size_deg));
        var lat_max = WebMercatorProjection.YToLatitude(180 - (y * tile_size_deg));

        var tile_info = new TileInfo
        {
            Min = new Location(lat_min, lon_min),
            Max = new Location(lat_max, lon_max),
            TilePixelSize = MapProjection.TileSize
        };

#if DEBUG
        var timer = Stopwatch.StartNew();
#endif
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // ограничение по времени
            return await func(tile_info, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("FunctionalTileSource: z={0} x={1} y={2}: generation canceled", ZoomLevel, x, y);
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("FunctionalTileSource: z={0} x={1} y={2}: {3}", ZoomLevel, x, y, ex.Message);
            return null;
        }
#if DEBUG
        finally
        {
            timer.Stop();
            Debug.WriteLine("FunctionalTileSource: z={0} x={1} y={2} generated in {3} ms", ZoomLevel, x, y, timer.ElapsedMilliseconds);
        }
#endif
    }

    /// <summary>Генерирует событие сброса тайлового слоя, к которому данный источник присоединён</summary>
    public void ResetLayer() => OnReset(EventArgs.Empty);

    /// <summary>Команда сброса тайлового слоя</summary>
    public ICommand ResetLayerCommand => field ??= new LambdaCommand(_ => ResetLayer());
}
