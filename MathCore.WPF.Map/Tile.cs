using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MathCore.WPF.Map;

/// <summary>Тайл карты</summary>
[DebuggerDisplay("Tile[x:{X}y:{Y}z:{ZoomLevel}]")]
public sealed class Tile(int ZoomLevel, int X, int Y)
{
    /// <summary>Устанавливает изображение тайла с опциональной анимацией проявления</summary>
    /// <param name="ImageSource">Источник изображения</param>
    /// <param name="FadeIn">Включить плавное проявление</param>
    public void SetImage(ImageSource ImageSource, bool FadeIn = true)
    {
        Pending = false;

        if (FadeIn && FadeDuration > TimeSpan.Zero)
            if (ImageSource is BitmapSource { IsFrozen: false, IsDownloading: true } bitmap_source)
            {
                bitmap_source.DownloadCompleted += BitmapDownloadCompleted;
                bitmap_source.DownloadFailed += BitmapDownloadFailed;
            }
            else
                Image.BeginAnimation(UIElement.OpacityProperty, __FadeAnimation);
        else
            Image.Opacity = 1d;

        Image.Source = ImageSource;
    }

    private void BitmapDownloadCompleted(object? sender, EventArgs e)
    {
        var bitmap_source = (BitmapSource)sender!;

        bitmap_source.DownloadCompleted -= BitmapDownloadCompleted;
        bitmap_source.DownloadFailed -= BitmapDownloadFailed;

        Image.BeginAnimation(UIElement.OpacityProperty, __FadeAnimation);
    }

    private void BitmapDownloadFailed(object? sender, ExceptionEventArgs e)
    {
        var bitmap_source = (BitmapSource)sender!;

        bitmap_source.DownloadCompleted -= BitmapDownloadCompleted;
        bitmap_source.DownloadFailed -= BitmapDownloadFailed;

        Image.Source = null;
    }

    private static TimeSpan __FadeDuration = TimeSpan.FromSeconds(0.15);

    private static DoubleAnimation __FadeAnimation = new(1d, TimeSpan.FromSeconds(0.15));

    /// <summary>Длительность анимации проявления изображения тайла</summary>
    public static TimeSpan FadeDuration
    {
        get => __FadeDuration;
        set
        {
            if(__FadeDuration == value) return;
            __FadeDuration = value;
            __FadeAnimation = new(1d, value);
        }
    }

    /// <summary>Уровень масштаба тайла</summary>
    public int ZoomLevel { get; } = ZoomLevel;

    /// <summary>Индекс тайла по оси X (логический, без нормализации)</summary>
    public int X { get; } = X;

    /// <summary>Индекс тайла по оси Y</summary>
    public int Y { get; } = Y;

    /// <summary>Визуальный элемент изображения тайла</summary>
    public Image Image { get; } = new() { Opacity = 0d };

    /// <summary>Флаг ожидания загрузки изображения тайла</summary>
    public bool Pending { get; set; } = true;

    /// <summary>Индекс тайла по оси X с нормализацией в пределах доступного диапазона текущего уровня</summary>
    public int XIndex
    {
        get
        {
            var num_tiles = 1 << ZoomLevel;
            return (X % num_tiles + num_tiles) % num_tiles;
        }
    }

    /// <summary>Деконструктор кортежа (X, Y)</summary>
    /// <param name="X">Индекс X</param>
    /// <param name="Y">Индекс Y</param>
    public void Deconstruct(out int X, out int Y) => (X, Y) = (this.X, this.Y);

    /// <summary>Деконструктор кортежа (X, Y, ZoomLevel)</summary>
    /// <param name="X">Индекс X</param>
    /// <param name="Y">Индекс Y</param>
    /// <param name="ZoomLevel">Уровень масштаба</param>
    public void Deconstruct(out int X, out int Y, out int ZoomLevel) => (X, Y, ZoomLevel) = (this.X, this.Y, this.ZoomLevel);
}