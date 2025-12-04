using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.Extensions;

/// <summary>Перечислитель пикселей тайла с поддержкой преобразования координат</summary>
public ref struct TilePixelEnumerator
{
    private readonly TileInfo _Tile;
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
        _Tile = Tile;
        _X = -1;
        _Y = 0;
    }

    /// <summary>Текущая позиция пикселя с координатами</summary>
    public readonly PixelPosition Current => new(_X, _Y, _Tile.GetLocation(_X, _Y));

    /// <summary>Переход к следующему пикселю</summary>
    public bool MoveNext()
    {
        _X++;
        if (_X >= _Tile.TilePixelSize)
        {
            _X = 0;
            _Y++;
        }
        return _Y < _Tile.TilePixelSize;
    }

    /// <summary>Возвращает координаты для заданного индекса пикселя</summary>
    public readonly Location GetLocation(int X, int Y) => _Tile.GetLocation(X, Y);

    /// <summary>Возвращает перечислитель для поддержки foreach</summary>
    public TilePixelEnumerator GetEnumerator() => this;
}
