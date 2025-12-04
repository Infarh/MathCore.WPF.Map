using System.Windows;
using System.Windows.Media.Imaging;

namespace MathCore.WPF.Map.Infrastructure;

/// <summary>Обеспечивает доступ к пикселям WriteableBitmap для чтения и записи</summary>
public sealed class BitmapPixelAccessor : IDisposable
{
    /// <summary>Один пиксель изображения в формате BGRA</summary>
    public readonly ref struct Pixel
    {
        private readonly byte[] _Buffer;
        private readonly int _Index;

        internal Pixel(byte[] Buffer, int Index) => (_Buffer, _Index) = (Buffer, Index);

        /// <summary>Красный канал</summary>
        public byte R { get => _Buffer[_Index + 2]; set => _Buffer[_Index + 2] = value; }

        /// <summary>Зелёный канал</summary>
        public byte G { get => _Buffer[_Index + 1]; set => _Buffer[_Index + 1] = value; }

        /// <summary>Синий канал</summary>
        public byte B { get => _Buffer[_Index + 0]; set => _Buffer[_Index + 0] = value; }

        /// <summary>Альфа-канал (прозрачность)</summary>
        public byte A { get => _Buffer[_Index + 3]; set => _Buffer[_Index + 3] = value; }

        /// <summary>Устанавливает значения всех каналов пикселя</summary>
        public void Set(byte R, byte G, byte B, byte A)
        {
            _Buffer[_Index + 0] = B;
            _Buffer[_Index + 1] = G;
            _Buffer[_Index + 2] = R;
            _Buffer[_Index + 3] = A;
        }

        public void Deconstruct(out byte R, out byte G, out byte B, out byte A)
        {
            R = _Buffer[_Index + 2];
            G = _Buffer[_Index + 1];
            B = _Buffer[_Index + 0];
            A = _Buffer[_Index + 3];
        }
    }

    private readonly WriteableBitmap _Bitmap;
    private readonly byte[] _Pixels;
    private readonly int _Stride;
    private readonly int _Width;
    private readonly int _Height;
    private bool _Disposed;

    /// <summary>Создаёт accessor для работы с пикселями WriteableBitmap</summary>
    public BitmapPixelAccessor(WriteableBitmap Bitmap)
    {
        _Bitmap = Bitmap;
        _Width = Bitmap.PixelWidth;
        _Height = Bitmap.PixelHeight;
        _Stride = Bitmap.BackBufferStride;
        _Pixels = new byte[_Stride * _Height];
    }

    /// <summary>Ширина изображения в пикселях</summary>
    public int Width => _Width;

    /// <summary>Высота изображения в пикселях</summary>
    public int Height => _Height;

    /// <summary>Шаг строки в байтах</summary>
    public int Stride => _Stride;

    /// <summary>Изображение к которому предоставляется доступ</summary>
    public WriteableBitmap Bitmap => _Bitmap;

    /// <summary>Доступ к пикселю по координатам</summary>
    public Pixel this[int X, int Y]
    {
        get => new(_Pixels, Y * _Stride + X * 4);
        set
        {
            var index = Y * _Stride + X * 4;
            _Pixels[index + 0] = value.B;
            _Pixels[index + 1] = value.G;
            _Pixels[index + 2] = value.R;
            _Pixels[index + 3] = value.A;
        }
    }

    /// <summary>Применяет изменения к WriteableBitmap</summary>
    public void Flush()
    {
        _Bitmap.WritePixels(new Int32Rect(0, 0, _Width, _Height), _Pixels, _Stride, 0);
    }

    /// <summary>Очищает все пиксели указанным цветом</summary>
    public void Clear(byte R = 0, byte G = 0, byte B = 0, byte A = 0)
    {
        for (var i = 0; i < _Pixels.Length; i += 4)
        {
            _Pixels[i + 0] = B;
            _Pixels[i + 1] = G;
            _Pixels[i + 2] = R;
            _Pixels[i + 3] = A;
        }
    }

    public void Dispose()
    {
        if (_Disposed) return;
        Flush();
        _Disposed = true;
    }
}
