using MathCore.WPF.Map.Extensions;
using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.Examples;

/// <summary>Примеры использования BitmapPixelAccessor</summary>
public static class BitmapPixelAccessorExamples
{
    /// <summary>Пример 1: Простой градиент</summary>
    public static FunctionalTileSourceDelegate CreateGradientTileFunc()
    {
        return async (tile, cancel) =>
        {
            using var accessor = tile.CreatePixelAccessor();

            for (var y = 0; y < accessor.Height; y++)
            {
                cancel.ThrowIfCancellationRequested();
                var v = (byte)(255.0 * y / (accessor.Height - 1));

                for (var x = 0; x < accessor.Width; x++)
                    accessor[x, y].Set(R: v, G: v, B: v, A: 255);
            }

            return accessor.Bitmap;
        };
    }

    /// <summary>Пример 2: Шахматная доска</summary>
    public static FunctionalTileSourceDelegate CreateCheckerboardTileFunc(int CellSize = 32)
    {
        return async (tile, cancel) =>
        {
            using var accessor = tile.CreatePixelAccessor();

            for (var y = 0; y < accessor.Height; y++)
            {
                cancel.ThrowIfCancellationRequested();

                for (var x = 0; x < accessor.Width; x++)
                {
                    var is_white = ((x / CellSize) + (y / CellSize)) % 2 == 0;
                    var color = is_white ? (byte)255 : (byte)0;
                    accessor[x, y].Set(color, color, color, 255);
                }
            }

            return accessor.Bitmap;
        };
    }

    /// <summary>Пример 3: Тепловая карта на основе расстояния от центра</summary>
    public static FunctionalTileSourceDelegate CreateHeatmapTileFunc(Location Center)
    {
        return async (tile, cancel) =>
        {
            using var accessor = tile.CreatePixelAccessor();

            var lat_min = tile.Min.Latitude;
            var lat_max = tile.Max.Latitude;
            var lon_min = tile.Min.Longitude;
            var lon_max = tile.Max.Longitude;
            var size = tile.TilePixelSize;

            for (var y = 0; y < size; y++)
            {
                cancel.ThrowIfCancellationRequested();
                var lat = lat_max - (lat_max - lat_min) * y / (size - 1.0);

                for (var x = 0; x < size; x++)
                {
                    var lon = lon_min + (lon_max - lon_min) * x / (size - 1.0);
                    var loc = new Location(lat, lon);

                    var distance_rad = Projections.Base.AzimuthalProjection
                        .GetAzimuthDistance(Center, loc).Distance;
                    var distance_km = distance_rad * Projections.Base.MapProjection.Wgs84EquatorialRadius / 1000.0;

                    var normalized = Math.Min(1.0, distance_km / 1000.0); // нормализация до 1000 км
                    var (r, g, b) = HeatmapColor(normalized);

                    accessor[x, y].Set(r, g, b, 200);
                }
            }

            return accessor.Bitmap;
        };
    }

    /// <summary>Пример 4: Использование вспомогательной функции рисования</summary>
    public static FunctionalTileSourceDelegate CreateCustomPattern()
    {
        return async (tile, cancel) =>
        {
            using var accessor = tile.CreatePixelAccessor();

            // Очистка фона
            accessor.Clear(R: 255, G: 255, B: 255, A: 255);

            // Рисование диагональных линий
            DrawDiagonalLines(accessor, 10, (r: 255, g: 0, b: 0, a: 255));

            return accessor.Bitmap;
        };
    }

    /// <summary>Пример 5: Передача accessor в другую функцию</summary>
    private static void DrawDiagonalLines(
        BitmapPixelAccessor Accessor,
        int Step,
        (byte r, byte g, byte b, byte a) Color)
    {
        for (var y = 0; y < Accessor.Height; y++)
            for (var x = 0; x < Accessor.Width; x++)
                if ((x + y) % Step == 0)
                    Accessor[x, y].Set(Color.r, Color.g, Color.b, Color.a);
    }

    /// <summary>Пример 6: Возврат accessor из функции для дальнейшей обработки</summary>
    public static BitmapPixelAccessor CreateAccessorWithBackground(int Width, int Height, byte R, byte G, byte B)
    {
        var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
            Width, Height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);

        var accessor = new BitmapPixelAccessor(bitmap);
        accessor.Clear(R, G, B, 255);

        return accessor;
    }

    /// <summary>Преобразование нормализованного значения [0..1] в цвет тепловой карты</summary>
    private static (byte R, byte G, byte B) HeatmapColor(double Value)
    {
        Value = Math.Max(0, Math.Min(1, Value));

        double r, g, b;
        if (Value < 0.25)
        {
            var t = Value / 0.25;
            r = 0; g = t * 255; b = 255;
        }
        else if (Value < 0.5)
        {
            var t = (Value - 0.25) / 0.25;
            r = 0; g = 255; b = (1 - t) * 255;
        }
        else if (Value < 0.75)
        {
            var t = (Value - 0.5) / 0.25;
            r = t * 255; g = 255; b = 0;
        }
        else
        {
            var t = (Value - 0.75) / 0.25;
            r = 255; g = (1 - t) * 255; b = 0;
        }

        return ((byte)r, (byte)g, (byte)b);
    }
}
