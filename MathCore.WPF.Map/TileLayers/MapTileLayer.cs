using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Projections;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Панель тайлов карты</summary>
[ContentProperty("TileSource")]
public class MapTileLayer : Panel, IMapLayer
{
    static MapTileLayer() => IsHitTestVisibleProperty.OverrideMetadata(typeof(MapTileLayer), new FrameworkPropertyMetadata(false));

    /// <summary>Слой OpenStreetMap</summary>
    public static MapTileLayer OpenStreetMapTileLayer =>
        new()
        {
            SourceName = "OpenStreetMap",
            Description = "© [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)",
            TileSource = new() { UriFormat = "http://{c}.tile.openstreetmap.org/{z}/{x}/{y}.png" },
            MaxZoomLevel = 19
        };

    #region Projection : MapProjection - Проекция слоя

    /// <summary>Проекция слоя</summary>
    //[Category("")]
    [Description("Проекция слоя")]
    public MapProjection Projection
    {
        get => (MapProjection)GetValue(ProjectionProperty);
        set => SetValue(ProjectionProperty, value);
    }

    /// <summary>Проекция слоя</summary>
    public static readonly DependencyProperty ProjectionProperty =
        DependencyProperty.Register(
            nameof(Projection),
            typeof(MapProjection),
            typeof(MapTileLayer),
            new(default(MapProjection)));

    #endregion

    #region TileSource : TileSource - Источник данных слоя

    /// <summary>Источник данных слоя</summary>
    public TileSource? TileSource
    {
        get => (TileSource?)GetValue(TileSourceProperty);
        set => SetValue(TileSourceProperty, value);
    }

    public static readonly DependencyProperty TileSourceProperty = DependencyProperty
        .Register(
            nameof(TileSource),
            typeof(TileSource),
            typeof(MapTileLayer),
            new(null, (o, _) => ((MapTileLayer)o).TileSourcePropertyChanged()));

    #endregion

    #region SourceName : string - Название слоя. Используется как ключ для кеширования данных

    /// <summary>Название слоя. Используется как ключ для кеширования данных</summary>
    public string SourceName
    {
        get => (string)GetValue(SourceNameProperty);
        set => SetValue(SourceNameProperty, value);
    }

    public static readonly DependencyProperty SourceNameProperty = DependencyProperty
        .Register(
            nameof(SourceName),
            typeof(string),
            typeof(MapTileLayer),
            new(null));

    #endregion

    #region Description : string - Описание слоя. Может выводиться на карту

    /// <summary>Описание слоя. Может выводиться на карту</summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty
        .Register(
            nameof(Description),
            typeof(string),
            typeof(MapTileLayer),
            new(null));

    #endregion

    #region ZoomLevelOffset : double - Смещение уровня сетки масштаба слоя относительно масштаба компонента карты

    /// <summary>Смещение уровня сетки масштаба слоя относительно масштаба компонента карты</summary>
    public double ZoomLevelOffset
    {
        get => (double)GetValue(ZoomLevelOffsetProperty);
        set => SetValue(ZoomLevelOffsetProperty, value);
    }

    public static readonly DependencyProperty ZoomLevelOffsetProperty = DependencyProperty
        .Register(
            nameof(ZoomLevelOffset),
            typeof(double),
            typeof(MapTileLayer),
            new(0d, (o, _) => ((MapTileLayer)o).UpdateTileGrid()));

    #endregion

    #region MinZoomLevel : int - Минимальный уровень шкалы масштаба слоя

    /// <summary>Минимальный уровень шкалы масштаба слоя</summary>
    public int MinZoomLevel
    {
        get => (int)GetValue(MinZoomLevelProperty);
        set => SetValue(MinZoomLevelProperty, value);
    }

    public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty
        .Register(
            nameof(MinZoomLevel),
            typeof(int),
            typeof(MapTileLayer),
            new(0));

    #endregion

    #region MaxZoomLevel : int - Максимальный уровень шкалы масштаба слоя

    /// <summary>Максимальный уровень шкалы масштаба слоя</summary>
    public int MaxZoomLevel
    {
        get => (int)GetValue(MaxZoomLevelProperty);
        set => SetValue(MaxZoomLevelProperty, value);
    }

    public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty
        .Register(
            nameof(MaxZoomLevel),
            typeof(int),
            typeof(MapTileLayer),
            new(18));

    #endregion

    #region MaxParallelDownloads : int - Максимальное число потоков для скачивания данных с сервера

    /// <summary>Максимальное число потоков для скачивания данных с сервера</summary>
    public int MaxParallelDownloads
    {
        get => (int)GetValue(MaxParallelDownloadsProperty);
        set => SetValue(MaxParallelDownloadsProperty, value);
    }

    public static readonly DependencyProperty MaxParallelDownloadsProperty = DependencyProperty
        .Register(
            nameof(MaxParallelDownloads),
            typeof(int),
            typeof(MapTileLayer),
            new(4));

    #endregion

    #region UpdateInterval : TimeSpan - Минимальный интервал времени задержки перед обновлением данных слоя

    /// <summary>Минимальный интервал времени задержки перед обновлением данных слоя</summary>
    public TimeSpan UpdateInterval
    {
        get => (TimeSpan)GetValue(UpdateIntervalProperty);
        set => SetValue(UpdateIntervalProperty, value);
    }

    public static readonly DependencyProperty UpdateIntervalProperty = DependencyProperty
        .Register(
            nameof(UpdateInterval),
            typeof(TimeSpan),
            typeof(MapTileLayer),
            new(TimeSpan.FromSeconds(0.2), (o, e) => ((MapTileLayer)o)._UpdateTimer.Interval = (TimeSpan)e.NewValue));

    #endregion

    #region UpdateWhileViewportChanging : bool - Выполнять обновление данных слоя в процессе изменения положения карты на экране

    /// <summary>Выполнять обновление данных слоя в процессе изменения положения карты на экране</summary>
    public bool UpdateWhileViewportChanging
    {
        get => (bool)GetValue(UpdateWhileViewportChangingProperty);
        set => SetValue(UpdateWhileViewportChangingProperty, value);
    }

    public static readonly DependencyProperty UpdateWhileViewportChangingProperty = DependencyProperty
        .Register(
            nameof(UpdateWhileViewportChanging),
            typeof(bool),
            typeof(MapTileLayer),
            new(true));

    #endregion

    #region MapBackground : Brush - Подложка карты

    /// <summary>Подложка карты</summary>
    public Brush MapBackground
    {
        get => (Brush)GetValue(MapBackgroundProperty);
        set => SetValue(MapBackgroundProperty, value);
    }

    public static readonly DependencyProperty MapBackgroundProperty = DependencyProperty
        .Register(
            nameof(MapBackground),
            typeof(Brush),
            typeof(MapTileLayer),
            new(null));

    #endregion

    #region MapForeground : Brush - Кисть рисования объектов на поверхности карты

    /// <summary>Кисть рисования объектов на поверхности карты</summary>
    public Brush MapForeground
    {
        get => (Brush)GetValue(MapForegroundProperty);
        set => SetValue(MapForegroundProperty, value);
    }

    public static readonly DependencyProperty MapForegroundProperty = DependencyProperty
        .Register(
            nameof(MapForeground),
            typeof(Brush),
            typeof(MapTileLayer),
            new(null));

    #endregion

    /// <summary>Таймер обновления данных слоя</summary>
    private readonly DispatcherTimer _UpdateTimer;

    /// <summary>Карта, которой принадлежит слой</summary>
    private MapBase? _ParentMap;

    /// <summary>Объект, выполняющий загрузку изображений слоя</summary>
    public ITileImageLoader TileImageLoader { get; }

    /// <summary>Коллекция тайлов соля</summary>
    public TilesCollection Tiles { get; private set; } = [];

    /// <summary>Сетка размещения тайлов слоя</summary>
    public TileGrid? TileGrid { get; private set; }

    /// <summary>Карта, которой принадлежит слой</summary>
    public MapBase? ParentMap
    {
        get => _ParentMap;
        set
        {
            if (_ParentMap is not null)
                _ParentMap.ViewportChanged -= OnViewportChanged;

            _ParentMap = value;

            if (_ParentMap is not null)
                _ParentMap.ViewportChanged += OnViewportChanged;

            UpdateTileGrid();
        }
    }

    public MapTileLayer() : this(new TileImageLoader()) { }

    public MapTileLayer(ITileImageLoader TileImageLoader)
    {
        IsHitTestVisible = false;
        RenderTransform = new MatrixTransform();
        this.TileImageLoader = TileImageLoader;

        _UpdateTimer = new() { Interval = UpdateInterval };
        _UpdateTimer.Tick += (_, _) => UpdateTileGrid();

        MapPanel.InitMapElement(this);
    }

    /// <summary>Метод, выполняющий измерение размеров слоя и его дочерних элементов</summary>
    /// <param name="AvailableSize">Доступный размер</param>
    /// <returns>Пустой размер, указывающий ограничения размеров нет</returns>
    protected override Size MeasureOverride(Size AvailableSize)
    {
        var available_size = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (UIElement element in Children)
            element.Measure(available_size);

        return new();
    }

    protected override Size ArrangeOverride(Size FinalSize)
    {
        if (TileGrid is not { ZoomLevel: var zoom, XMin: var x_min, YMin: var y_min })
            return FinalSize;

        foreach (var tile in Tiles)
        {
            var tile_size = MapProjection.TileSize << zoom - tile.ZoomLevel;
            var x = tile_size * tile.X - MapProjection.TileSize * x_min;
            var y = tile_size * tile.Y - MapProjection.TileSize * y_min;

            tile.Image.Width = tile_size;
            tile.Image.Height = tile_size;
            tile.Image.Arrange(new(x, y, tile_size, tile_size));
        }

        return FinalSize;
    }

    protected virtual void UpdateTileGrid()
    {
        _UpdateTimer.Stop();

        if (_ParentMap is { LayerMapProjection.IsWebMercator: true })
        {
            var tile_grid = GetTileGrid();

            if (tile_grid.Equals(TileGrid)) return;
            TileGrid = tile_grid;
            SetRenderTransform();
        }
        else
            TileGrid = null;

        UpdateTiles();
    }

    private void TileSourcePropertyChanged()
    {
        if (TileGrid is null) return;
        Tiles.Clear();
        UpdateTiles();
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        if (TileGrid is null || e.ProjectionChanged || Math.Abs(e.LongitudeOffset) > 180d)
            UpdateTileGrid();
        else
        {
            SetRenderTransform();

            if (_UpdateTimer.IsEnabled && !UpdateWhileViewportChanging)
                _UpdateTimer.Stop();

            if (!_UpdateTimer.IsEnabled)
                _UpdateTimer.Start();
        }
    }

    private TileGrid GetTileGrid()
    {
        var tile_zoom_level = Math.Max(0, (int)Math.Round(_ParentMap!.ZoomLevel + ZoomLevelOffset));
        var tile_scale = (double)(1 << tile_zoom_level);
        var scale = tile_scale / (Math.Pow(2d, _ParentMap.ZoomLevel) * MapProjection.TileSize);
        var tile_center_x = tile_scale * (0.5 + _ParentMap.Center.Longitude / 360d);
        var tile_center_y = tile_scale * (0.5 - WebMercatorProjection.LatitudeToY(_ParentMap.Center.Latitude) / 360d);
        var view_center_x = _ParentMap.RenderSize.Width / 2d;
        var view_center_y = _ParentMap.RenderSize.Height / 2d;

        var transform = new MatrixTransform
        {
            Matrix = MatrixEx.TranslateScaleRotateTranslate(
                Translation1X: -view_center_x,
                Translation1Y: -view_center_y,
                       ScaleX: scale,
                       ScaleY: scale,
                RotationAngle: -_ParentMap.Heading,
                Translation2X: tile_center_x,
                Translation2Y: tile_center_y)
        };

        var bounds = transform.TransformBounds(new(0d, 0d, _ParentMap.RenderSize.Width, _ParentMap.RenderSize.Height));

        return new(
            ZoomLevel: tile_zoom_level,
                 XMin: (int)Math.Floor(bounds.X),
                 YMin: (int)Math.Floor(bounds.Y),
                 XMax: (int)Math.Floor(bounds.X + bounds.Width),
                 YMax: (int)Math.Floor(bounds.Y + bounds.Height));
    }

    private void SetRenderTransform()
    {
        var tile_scale = (double)(1 << TileGrid!.ZoomLevel);
        var scale = Math.Pow(2d, _ParentMap!.ZoomLevel) / tile_scale;
        var tile_center_x = tile_scale * (0.5 + _ParentMap.Center.Longitude / 360d);
        var tile_center_y = tile_scale * (0.5 - WebMercatorProjection.LatitudeToY(_ParentMap.Center.Latitude) / 360d);
        var tile_origin_x = MapProjection.TileSize * (tile_center_x - TileGrid.XMin);
        var tile_origin_y = MapProjection.TileSize * (tile_center_y - TileGrid.YMin);
        var view_center_x = _ParentMap.RenderSize.Width / 2d;
        var view_center_y = _ParentMap.RenderSize.Height / 2d;

        ((MatrixTransform)RenderTransform).Matrix = MatrixEx.TranslateScaleRotateTranslate(
            Translation1X: -tile_origin_x,
            Translation1Y: -tile_origin_y,
                   ScaleX: scale,
                   ScaleY: scale,
            RotationAngle: _ParentMap.Heading,
            Translation2X: view_center_x,
            Translation2Y: view_center_y);
    }

    private void UpdateTiles()
    {
        var new_tiles = new TilesCollection();

        if (_ParentMap is { MapLayer: var layer }
            && TileGrid is
            {
                ZoomLevel: var zoom_level,
                XMin: var x_min,
                XMax: var x_max,
                YMin: var y_min,
                YMax: var y_max,
            }
            && TileSource is not null)
        {
            var max_zoom_level = Math.Min(zoom_level, MaxZoomLevel);
            var min_zoom_level = MinZoomLevel;

            if (min_zoom_level < max_zoom_level && !ReferenceEquals(layer, this))
                min_zoom_level = max_zoom_level;

            for (var z = min_zoom_level; z <= max_zoom_level; z++)
            {
                var tile_size = 1 << zoom_level - z;
                var x1 = (int)Math.Floor((double)x_min / tile_size);
                var x2 = x_max / tile_size;
                var y1 = Math.Max(y_min / tile_size, 0);
                var y2 = Math.Min(y_max / tile_size, (1 << z) - 1);

                for (var y = y1; y <= y2; y++)
                    for (var x = x1; x <= x2; x++)
                        new_tiles.Add(Tiles[z, x, y] ?? new(z, x, y));
            }
        }

        Tiles = new_tiles;

        Children.Clear();

        foreach (var tile in Tiles)
            Children.Add(tile.Image);

        _ = TileImageLoader.LoadTilesAsync(this);
    }
}