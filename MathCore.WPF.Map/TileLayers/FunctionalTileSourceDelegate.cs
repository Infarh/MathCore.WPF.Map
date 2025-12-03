using System.Windows.Media;

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