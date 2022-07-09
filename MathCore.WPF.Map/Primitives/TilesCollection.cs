using System.Collections;

namespace MathCore.WPF.Map.Primitives;

public sealed class TilesCollection : IEnumerable<Tile>
{
    private readonly Dictionary<(int Zoom, int X, int Y), Tile> _Tiles = new();

    public Tile? this[int Zoom, int X, int Y] => _Tiles.TryGetValue((Zoom, X, Y), out var tile) ? tile : null;

    public void Add(Tile tile) => _Tiles[(tile.ZoomLevel, tile.X, tile.Y)] = tile;

    public Tile? Similar(Tile tile) => this[tile.ZoomLevel, tile.X, tile.Y];

    public void Clear() => _Tiles.Clear();

    public IEnumerator<Tile> GetEnumerator() => _Tiles.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this).GetEnumerator();
}
