using MathCore.WPF.Map.Infrastructure;

namespace MathCore.WPF.Map;

/// <summary>Прямоугольная сетка тайлов для текущего представления</summary>
public sealed class TileGrid(int ZoomLevel, int XMin, int YMin, int XMax, int YMax) : IEquatable<TileGrid?>
{
    /// <summary>Уровень масштаба сетки</summary>
    public int ZoomLevel { get; } = ZoomLevel;

    /// <summary>Минимальный индекс X</summary>
    public int XMin { get; } = XMin;

    /// <summary>Минимальный индекс Y</summary>
    public int YMin { get; } = YMin;

    /// <summary>Максимальный индекс X</summary>
    public int XMax { get; } = XMax;

    /// <summary>Максимальный индекс Y</summary>
    public int YMax { get; } = YMax;

    /// <summary>Сравнение на равенство по полям сетки</summary>
    /// <param name="TileGrid">Другая сетка</param>
    /// <returns>Истина, если все поля совпадают</returns>
    public bool Equals(TileGrid? TileGrid) =>
        TileGrid is not null && 
        TileGrid.ZoomLevel == ZoomLevel &&
        TileGrid.XMin == XMin && 
        TileGrid.YMin == YMin && 
        TileGrid.XMax == XMax && 
        TileGrid.YMax == YMax;

    /// <summary>Сравнение с произвольным объектом</summary>
    public override bool Equals(object? obj) => Equals(obj as TileGrid);

    /// <summary>Хэш‑код по основным полям</summary>
    public override int GetHashCode() =>
#if NET5_0_OR_GREATER
        HashCode.Combine(ZoomLevel, XMin, YMin, XMax, YMax);
#else
        HashBuilder.Create().Append(ZoomLevel).Append(XMin).Append(YMin).Append(YMax);
#endif
}