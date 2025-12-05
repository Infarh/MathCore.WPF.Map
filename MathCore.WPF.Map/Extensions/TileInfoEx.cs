using System.Windows.Media;
using System.Windows.Media.Imaging;

using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.Extensions;

public static class TileInfoEx
{
    extension(TileInfo tile)
    {
        public WriteableBitmap CreateBitmap()
        {
            var tile_size = tile.TilePixelSize;
            return new WriteableBitmap(tile_size, tile_size, 96, 96, PixelFormats.Bgra32, null);
        }

        /// <summary>Создаёт accessor для работы с пикселями тайла</summary>
        public BitmapPixelAccessor CreatePixelAccessor() => new(tile.CreateBitmap());

        /// <summary>Создаёт перечислитель пикселей тайла с автоматическим преобразованием координат</summary>
        public TilePixelEnumerator GetEnumerator() => new(tile);
    }
}
