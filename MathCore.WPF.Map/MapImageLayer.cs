using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;

namespace MathCore.WPF.Map;

/// <summary>Слой карты на основе изображений</summary>
public abstract class MapImageLayer : MapPanel, IMapLayer
{
    #region Property MinLatitude : double - Минимальное значение широты

    public static readonly DependencyProperty MinLatitudeProperty = DependencyProperty
       .Register(
            nameof(MinLatitude),
            typeof(double),
            typeof(MapImageLayer),
            new(double.NaN));

    /// <summary>Минимальное значение широты. По умолчанию NaN.</summary>
    public double MinLatitude
    {
        get => (double)GetValue(MinLatitudeProperty);
        set => SetValue(MinLatitudeProperty, value);
    }

    #endregion

    #region Property MaxLatitude : double - Максимальное значение широты

    public static readonly DependencyProperty MaxLatitudeProperty = DependencyProperty
       .Register(
            nameof(MaxLatitude),
            typeof(double),
            typeof(MapImageLayer),
            new(double.NaN));

    /// <summary>Максимальное значение широты. По умолчанию NaN.</summary>
    public double MaxLatitude
    {
        get => (double)GetValue(MaxLatitudeProperty);
        set => SetValue(MaxLatitudeProperty, value);
    }

    #endregion

    #region Property MinLongitude : double - Минимальное значение долготы

    public static readonly DependencyProperty MinLongitudeProperty = DependencyProperty
       .Register(
            nameof(MinLongitude),
            typeof(double),
            typeof(MapImageLayer),
            new(double.NaN));

    /// <summary>Минимальное значение долготы. По умолчанию NaN.</summary>
    public double MinLongitude
    {
        get => (double)GetValue(MinLongitudeProperty);
        set => SetValue(MinLongitudeProperty, value);
    }

    #endregion

    #region Property MaxLongitude : double - Минимальное значение долготы

    public static readonly DependencyProperty MaxLongitudeProperty = DependencyProperty
       .Register(
            nameof(MaxLongitude),
            typeof(double),
            typeof(MapImageLayer),
            new(double.NaN));

    /// <summary>Минимальное значение долготы. По умолчанию NaN.</summary>
    public double MaxLongitude
    {
        get => (double)GetValue(MaxLongitudeProperty);
        set => SetValue(MaxLongitudeProperty, value);
    }

    #endregion

    #region Property MaxBoundingBoxWidth : double - Максимальная ширина области изображения карты

    public static readonly DependencyProperty MaxBoundingBoxWidthProperty = DependencyProperty
      .Register(
           nameof(MaxBoundingBoxWidth),
           typeof(double),
           typeof(MapImageLayer),
           new(double.NaN));

    /// <summary>Максимальная ширина области изображения карты. По умолчанию NaN.</summary>
    public double MaxBoundingBoxWidth
    {
        get => (double)GetValue(MaxBoundingBoxWidthProperty);
        set => SetValue(MaxBoundingBoxWidthProperty, value);
    }

    #endregion

    #region Property RelativeImageSize : double - Относительный размер изображения карты в соотношении с текущим размером визуальной части

    public static readonly DependencyProperty RelativeImageSizeProperty = DependencyProperty
      .Register(
           nameof(RelativeImageSize),
           typeof(double),
           typeof(MapImageLayer),
           new(1d));

    /// <summary>Относительный размер изображения карты в соотношении с текущим размером визуальной части</summary>
    /// <remarks>
    /// Установка значения больше 1 позволит <see cref="MapImageLayer"/> запросить изображения,
    /// которые больше чем визуальный размер компонента, для обеспечения плавного панорамирования.
    /// </remarks>
    public double RelativeImageSize
    {
        get => (double)GetValue(RelativeImageSizeProperty);
        set => SetValue(RelativeImageSizeProperty, value);
    }

    #endregion

    #region Property UpdateInterval : TimeSpan - Минимальное время между обновлениями изображений

    public static readonly DependencyProperty UpdateIntervalProperty = DependencyProperty
       .Register(
            nameof(UpdateInterval),
            typeof(TimeSpan),
            typeof(MapImageLayer),
            new(
                TimeSpan.FromSeconds(0.2),
                (o, e) => ((MapImageLayer)o)._UpdateTimer.Interval = (TimeSpan)e.NewValue));

    /// <summary>Минимальное время между обновлениями изображений</summary>
    public TimeSpan UpdateInterval
    {
        get => (TimeSpan)GetValue(UpdateIntervalProperty);
        set => SetValue(UpdateIntervalProperty, value);
    }

    #endregion

    #region Property UpdateWhileViewportChanging : bool - Обновлять изображения при изменении элемента управления

    public static readonly DependencyProperty UpdateWhileViewportChangingProperty = DependencyProperty
       .Register(
            nameof(UpdateWhileViewportChanging),
            typeof(bool),
            typeof(MapImageLayer),
            new(false));

    /// <summary>Обновлять изображения при изменении элемента управления</summary>
    public bool UpdateWhileViewportChanging
    {
        get => (bool)GetValue(UpdateWhileViewportChangingProperty);
        set => SetValue(UpdateWhileViewportChangingProperty, value);
    }

    #endregion

    #region Property Description : string - Описание

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty
       .Register(
            nameof(Description),
            typeof(string),
            typeof(MapImageLayer),
            new(null));

    /// <summary>Описание</summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    #endregion

    #region Property MapBackground : Brush - Подложка карты

    public static readonly DependencyProperty MapBackgroundProperty = DependencyProperty
       .Register(
            nameof(MapBackground),
            typeof(Brush),
            typeof(MapImageLayer),
            new(null));

    /// <summary>Подложка карты</summary>
    public Brush MapBackground
    {
        get => (Brush)GetValue(MapBackgroundProperty);
        set => SetValue(MapBackgroundProperty, value);
    }

    #endregion

    #region Property MapForeground : Brush - Основная кисть слоя

    public static readonly DependencyProperty MapForegroundProperty = DependencyProperty
      .Register(
           nameof(MapForeground),
           typeof(Brush),
           typeof(MapImageLayer),
           new(null));

    /// <summary>Основная кисть слоя</summary>
    public Brush MapForeground
    {
        get => (Brush)GetValue(MapForegroundProperty);
        set => SetValue(MapForegroundProperty, value);
    }

    #endregion

    private readonly DispatcherTimer _UpdateTimer;

    private BoundingBox _BoundingBox = null!;

    private int _TopImageIndex;

    private bool _UpdateInProgress;

    protected MapImageLayer()
    {
        Children.Add(new Image { Opacity = 0, Stretch = Stretch.Fill });
        Children.Add(new Image { Opacity = 0, Stretch = Stretch.Fill });

        _UpdateTimer = new DispatcherTimer().WithTick(UpdateInterval, UpdateImage);
    }

    protected void UpdateImage(ImageSource? ImageSource)
    {
        SetTopImage(ImageSource);

        if (ImageSource is BitmapSource { IsFrozen: false, IsDownloading: true } bitmap_source)
        {
            bitmap_source.DownloadCompleted += BitmapDownloadCompleted;
            bitmap_source.DownloadFailed += BitmapDownloadFailed;
        }
        else
            SwapImages();
    }

    private void BitmapDownloadCompleted(object? sender, EventArgs e)
    {
        var bitmap_source = (BitmapSource)sender!;

        bitmap_source.DownloadCompleted -= BitmapDownloadCompleted;
        bitmap_source.DownloadFailed -= BitmapDownloadFailed;

        SwapImages();
    }

    private void BitmapDownloadFailed(object? sender, ExceptionEventArgs e)
    {
        var bitmap_source = (BitmapSource)sender!;

        bitmap_source.DownloadCompleted -= BitmapDownloadCompleted;
        bitmap_source.DownloadFailed -= BitmapDownloadFailed;

        ((Image)Children[_TopImageIndex]).Source = null;
        SwapImages();
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e)
    {
        base.OnViewportChanged(e);

        if (e.ProjectionChanged)
        {
            UpdateImage(null);
            UpdateImage();
        }
        else
        {
            if (Math.Abs(e.LongitudeOffset) > 180 && _BoundingBox is { HasValidBounds: true })
            {
                var offset = 360 * Math.Sign(e.LongitudeOffset);

                _BoundingBox.West += offset;
                _BoundingBox.East += offset;

                foreach (UIElement element in Children)
                    if (GetBoundingBox(element) is { HasValidBounds: true, South: var south, West: var west, North: var north, East: var east })
                        SetBoundingBox(element, new(north, east + offset, south, west + offset));
            }

            if (_UpdateTimer.IsEnabled && !UpdateWhileViewportChanging)
                _UpdateTimer.Stop();

            if (!_UpdateTimer.IsEnabled)
                _UpdateTimer.Start();
        }
    }

    protected virtual void UpdateImage()
    {
        _UpdateTimer.Stop();

        if (_UpdateInProgress)
            _UpdateTimer.Start();
        else if (ParentMap is
        {
            RenderSize:
            {
                Width: > 0 and var map_width,
                Height: > 0 and var map_height
            },
            LayerMapProjection: var projection
        })
        {
            _UpdateInProgress = true;

            var width = map_width * RelativeImageSize;
            var height = map_height * RelativeImageSize;
            var x = (map_width - width) / 2;
            var y = (map_height - height) / 2;
            var rect = new Rect(x, y, width, height);

            _BoundingBox = projection.ViewportRectToBoundingBox(rect);

            if (_BoundingBox is
                {
                    HasValidBounds: true,
                    South: var south,
                    West: var west,
                    North: var north,
                    East: var east,
                    Width: var bounding_box_width
                })
            {
                if (!double.IsNaN(MinLatitude) && south < MinLatitude)
                    _BoundingBox.South = MinLatitude;

                if (!double.IsNaN(MinLongitude) && west < MinLongitude)
                    _BoundingBox.West = MinLongitude;

                if (!double.IsNaN(MaxLatitude) && north > MaxLatitude)
                    _BoundingBox.North = MaxLatitude;

                if (!double.IsNaN(MaxLongitude) && east > MaxLongitude)
                    _BoundingBox.East = MaxLongitude;

                if (!double.IsNaN(MaxBoundingBoxWidth) && bounding_box_width > MaxBoundingBoxWidth)
                {
                    var d = (bounding_box_width - MaxBoundingBoxWidth) / 2;
                    _BoundingBox.West += d;
                    _BoundingBox.East -= d;
                }
            }

            ImageSource? image_source = null;

            try
            {
                image_source = GetImage(_BoundingBox);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MapImageLayer: {0}", (object)ex.Message);
            }

            UpdateImage(image_source);
        }
    }

    /// <summary>Изображение для заданной области карты</summary>
    protected abstract ImageSource? GetImage(BoundingBox BoundingBox);

    private void SetTopImage(ImageSource? ImageSource)
    {
        _TopImageIndex = (_TopImageIndex + 1) % 2;
        var top_image = (Image)Children[_TopImageIndex];

        top_image.Source = ImageSource;
        SetBoundingBox(top_image, _BoundingBox?.Clone());
    }

    private void SwapImages()
    {
        var top_image = (Image)Children[_TopImageIndex];
        var bottom_image = (Image)Children[(_TopImageIndex + 1) % 2];

        SetZIndex(top_image, 1);
        SetZIndex(bottom_image, 0);

        if (top_image.Source is not null)
        {
            top_image.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = Tile.FadeDuration
            });

            bottom_image.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 0d,
                BeginTime = Tile.FadeDuration,
                Duration = TimeSpan.Zero
            });
        }
        else
        {
            top_image.Opacity = 0;
            bottom_image.Opacity = 0;
            bottom_image.Source = null;
        }

        _UpdateInProgress = false;
    }
}