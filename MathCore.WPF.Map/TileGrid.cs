using MathCore.WPF.Map.Infrastructure;

namespace MathCore.WPF.Map;

public sealed class TileGrid(int ZoomLevel, int XMin, int YMin, int XMax, int YMax) : IEquatable<TileGrid?>
{
    public int ZoomLevel { get; } = ZoomLevel;

    public int XMin { get; } = XMin;

    public int YMin { get; } = YMin;

    public int XMax { get; } = XMax;

    public int YMax { get; } = YMax;

    public bool Equals(TileGrid? TileGrid) =>
        TileGrid is not null && 
        TileGrid.ZoomLevel == ZoomLevel &&
        TileGrid.XMin == XMin && 
        TileGrid.YMin == YMin && 
        TileGrid.XMax == XMax && 
        TileGrid.YMax == YMax;

    public override bool Equals(object? obj) => Equals(obj as TileGrid);

    public override int GetHashCode() =>
#if NET5_0_OR_GREATER
        HashCode.Combine(ZoomLevel, XMin, YMin, XMax, YMax);
#else
        HashBuilder.Create().Append(ZoomLevel).Append(XMin).Append(YMin).Append(YMax);
#endif
}