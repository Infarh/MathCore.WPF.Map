using System.Windows.Media;

using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Делегат генерации изображения тайла</summary>
/// <param name="Tile">Информация о тайле включая географические границы и размер в пикселях</param>
/// <param name="Cancel">Токен отмены операции генерации тайла</param>
/// <returns>Асинхронный результат с изображением тайла или null, если тайл недоступен/операция отменена</returns>
public delegate Task<ImageSource?> FunctionalTileSourceDelegate(TileInfo Tile, CancellationToken Cancel);

/// <summary>Информация о тайле с географическими границами и размером</summary>
public readonly struct TileInfo
{
    /// <summary>Минимальная координата тайла (юго-западный угол)</summary>
    public Location Min { get; init; }

    /// <summary>Максимальная координата тайла (северо-восточный угол)</summary>
    public Location Max { get; init; }

    /// <summary>Размер тайла в пикселях</summary>
    public int TilePixelSize { get; init; }

    /// <summary>Ширина тайла по широте в градусах</summary>
    public double LatWidth => Max.Latitude - Min.Latitude;

    /// <summary>Ширина тайла по долготе в градусах</summary>
    public double LonWidth => Max.Longitude - Min.Longitude;

    /// <summary>Вычисляет широту для заданного индекса пикселя по вертикали</summary>
    /// <param name="Y">Индекс пикселя по вертикали (0 - верх тайла)</param>
    /// <returns>Широта в градусах</returns>
    public double GetLat(int Y) => Max.Latitude - (LatWidth * Y / (TilePixelSize - 1));

    /// <summary>Вычисляет долготу для заданного индекса пикселя по горизонтали</summary>
    /// <param name="X">Индекс пикселя по горизонтали (0 - левый край тайла)</param>
    /// <returns>Долгота в градусах</returns>
    public double GetLon(int X) => Min.Longitude + (LonWidth * X / (TilePixelSize - 1));

    /// <summary>Вычисляет географические координаты для заданного индекса пикселя</summary>
    /// <param name="X">Индекс пикселя по горизонтали</param>
    /// <param name="Y">Индекс пикселя по вертикали</param>
    /// <returns>Географические координаты центра пикселя</returns>
    public Location GetLocation(int X, int Y) => new(GetLat(Y), GetLon(X));
}