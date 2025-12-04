using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.Extensions;

/// <summary>Перечислитель пикселей тайла с поддержкой преобразования координат</summary>
public ref struct TilePixelEnumerator
{
    private readonly double _LatMin;
    private readonly double _LatMax;
    private readonly double _LonMin;
    private readonly double _LatDelta;
    private readonly double _LonDelta;
    private readonly int _TileSize;
    private int _X;
    private int _Y;

    /// <summary>Координаты текущего пикселя</summary>
    public readonly ref struct PixelPosition(int X, int Y, Location Location)
    {
        /// <summary>Индекс пикселя по горизонтали</summary>
        public int X { get; } = X;

        /// <summary>Индекс пикселя по вертикали</summary>
        public int Y { get; } = Y;

        /// <summary>Географические координаты центра пикселя</summary>
        public Location Location { get; } = Location;

        public void Deconstruct(out int X, out int Y, out Location Location) => (X, Y, Location) = (this.X, this.Y, this.Location);
    }

    internal TilePixelEnumerator(TileInfo Tile)
    {
        _LatMin = Tile.Min.Latitude;
        _LatMax = Tile.Max.Latitude;
        _LonMin = Tile.Min.Longitude;
        _LatDelta = Tile.LatWidth;
        _LonDelta = Tile.LonWidth;
        _TileSize = Tile.TilePixelSize;
        _X = -1;
        _Y = 0;
    }

    /// <summary>Текущая позиция пикселя с координатами</summary>
    public readonly PixelPosition Current
    {
        get
        {
            var lat = _LatMax - (_LatDelta * _Y / (_TileSize - 1));
            var lon = _LonMin + (_LonDelta * _X / (_TileSize - 1));
            return new(_X, _Y, new(lat, lon));
        }
    }

    /// <summary>Переход к следующему пикселю</summary>
    public bool MoveNext()
    {
        _X++;
        if (_X >= _TileSize)
        {
            _X = 0;
            _Y++;
        }
        return _Y < _TileSize;
    }

    /// <summary>Возвращает координаты для заданного индекса пикселя</summary>
    public readonly Location GetLocation(int X, int Y)
    {
        var lat = _LatMax - (_LatDelta * Y / (_TileSize - 1));
        var lon = _LonMin + (_LonDelta * X / (_TileSize - 1));
        return new(lat, lon);
    }

    /// <summary>Возвращает перечислитель для поддержки foreach</summary>
    public TilePixelEnumerator GetEnumerator() => this;
}
