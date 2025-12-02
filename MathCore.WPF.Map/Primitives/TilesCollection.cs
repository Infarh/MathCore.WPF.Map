using System.Collections;

namespace MathCore.WPF.Map.Primitives;

/// <summary>Коллекция тайлов карты с доступом по координатам и уровню приближения</summary>
public sealed class TilesCollection : IEnumerable<Tile>
{
    private readonly Dictionary<(int Zoom, int X, int Y), Tile> _Tiles = [];

    /// <summary>Возвращает тайл по координатам и уровню приближения или null, если он отсутствует</summary>
    /// <param name="Zoom">Уровень приближения</param>
    /// <param name="X">Координата X</param>
    /// <param name="Y">Координата Y</param>
    /// <returns>Тайл или null, если тайл не найден</returns>
    public Tile? this[int Zoom, int X, int Y] => _Tiles.TryGetValue((Zoom, X, Y), out var tile) ? tile : null;

    /// <summary>Добавляет тайл в коллекцию, перезаписывая существующий по тем же координатам и уровню</summary>
    /// <param name="tile">Добавляемый тайл</param>
    public void Add(Tile tile) => _Tiles[(tile.ZoomLevel, tile.X, tile.Y)] = tile;

    /// <summary>Ищет тайл с теми же координатами и уровнем приближения, что и указанный</summary>
    /// <param name="tile">Образец тайла</param>
    /// <returns>Найденный тайл или null</returns>
    public Tile? Similar(Tile tile) => this[tile.ZoomLevel, tile.X, tile.Y];

    /// <summary>Очищает коллекцию тайлов</summary>
    public void Clear() => _Tiles.Clear();

    /// <summary>Возвращает перечислитель по всем тайлам коллекции</summary>
    /// <returns>Перечислитель тайлов</returns>
    public IEnumerator<Tile> GetEnumerator() => _Tiles.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this).GetEnumerator();
}
