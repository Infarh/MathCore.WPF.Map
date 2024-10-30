using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MathCore.WPF.Map;

[DebuggerDisplay("Tile[x:{X}y:{Y}z:{ZoomLevel}]")]
public sealed class Tile(int ZoomLevel, int X, int Y)
{
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

    public int ZoomLevel { get; } = ZoomLevel;

    public int X { get; } = X;

    public int Y { get; } = Y;

    public Image Image { get; } = new() { Opacity = 0d };

    public bool Pending { get; set; } = true;

    public int XIndex
    {
        get
        {
            var num_tiles = 1 << ZoomLevel;
            return (X % num_tiles + num_tiles) % num_tiles;
        }
    }

    public void Deconstruct(out int X, out int Y) => (X, Y) = (this.X, this.Y);

    public void Deconstruct(out int X, out int Y, out int ZoomLevel) => (X, Y, ZoomLevel) = (this.X, this.Y, this.ZoomLevel);
}