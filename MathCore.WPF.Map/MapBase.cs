#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using MathCore.WPF.Map.Commands;
using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections;
using MathCore.WPF.Map.Projections.Base;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map;

/// <summary>Базовая логика компонента карты</summary>
public class MapBase : MapPanel
{
    private const double __MaximumZoomLevel = 22d;

    static MapBase()
    {
        ClipToBoundsProperty.OverrideMetadata(typeof(MapBase), new FrameworkPropertyMetadata(true));
        BackgroundProperty.OverrideMetadata(typeof(MapBase), new FrameworkPropertyMetadata(Brushes.Transparent));
    }

    /// <summary>Возникает при изменении состояния отображения карты</summary>
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    private const FillBehavior __AnimationFillBehavior = FillBehavior.Stop;

    #region Property Foreground : Brush - Кисть для рисования объектов на карте

    public static readonly DependencyProperty ForegroundProperty =
    Control.ForegroundProperty.AddOwner(typeof(MapBase));

    /// <summary>Кисть для рисования объектов на карте</summary>
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    #endregion

    #region Property Center : Location - Точка центра карты

    public static readonly DependencyProperty CenterProperty = DependencyProperty
        .Register(
            nameof(Center),
            typeof(Location),
            typeof(MapBase),
            new FrameworkPropertyMetadata(
                new Location(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).CenterPropertyChanged((Location)e.NewValue)));

    /// <summary>Точка центра карты</summary>
    public Location Center
    {
        get => (Location)GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

    #endregion

    #region Property TargetCenter : Location - Точка, относительно которой выполняется анимация

    public static readonly DependencyProperty TargetCenterProperty = DependencyProperty
        .Register(
            nameof(TargetCenter),
            typeof(Location),
            typeof(MapBase),
            new FrameworkPropertyMetadata(
                new Location(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).TargetCenterPropertyChanged((Location)e.NewValue)));

    /// <summary>Точка, относительно которой выполняется анимация</summary>
    public Location TargetCenter
    {
        get => (Location)GetValue(TargetCenterProperty);
        set => SetValue(TargetCenterProperty, value);
    }

    #endregion

    #region Property ZoomLevel : double - Уровень приближения карты

    public static readonly DependencyProperty ZoomLevelProperty = DependencyProperty
        .Register(
            nameof(ZoomLevel),
            typeof(double),
            typeof(MapBase),
            new FrameworkPropertyMetadata(
                1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).ZoomLevelPropertyChanged((double)e.NewValue),
                (o, e) =>
                {
                    if (o is not MapBase map) throw new InvalidOperationException();
                    if (e is not double v) throw new InvalidOperationException();
                    return Math.Max(map.MinZoomLevel, Math.Min(map.MaxZoomLevel, v));
                }),
            v => v is double and > 0 and < 100);

    /// <summary>Уровень приближения карты</summary>
    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    #endregion

    #region Property TargetZoomLevel : double - Целевое значение уровня приближения карты для процесса анимации

    public static readonly DependencyProperty TargetZoomLevelProperty = DependencyProperty
        .Register(
            nameof(TargetZoomLevel),
            typeof(double),
            typeof(MapBase),
            new FrameworkPropertyMetadata(
                1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).TargetZoomLevelPropertyChanged((double)e.NewValue)));

    /// <summary>Целевое значение уровня приближения карты для процесса анимации</summary>
    public double TargetZoomLevel
    {
        get => (double)GetValue(TargetZoomLevelProperty);
        set => SetValue(TargetZoomLevelProperty, value);
    }

    #endregion

    #region Property Heading : double - Угол поворота (курса) карты

    public static readonly DependencyProperty HeadingProperty = DependencyProperty
       .Register(
           nameof(Heading),
           typeof(double),
           typeof(MapBase),
           new FrameworkPropertyMetadata(
               0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
               (o, e) => ((MapBase)o).HeadingPropertyChanged((double)e.NewValue)));

    /// <summary>Угол поворота (курса) карты</summary>
    public double Heading
    {
        get => (double)GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    #endregion

    #region Property TargetHeading : double - Целевое значение угла поворота карты для процесса анимации

    public static readonly DependencyProperty TargetHeadingProperty = DependencyProperty
        .Register(
            nameof(TargetHeading),
            typeof(double),
            typeof(MapBase),
            new FrameworkPropertyMetadata(
                0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (o, e) => ((MapBase)o).TargetHeadingPropertyChanged((double)e.NewValue)));

    /// <summary>Целевое значение угла поворота карты для процесса анимации</summary>
    public double TargetHeading
    {
        get => (double)GetValue(TargetHeadingProperty);
        set => SetValue(TargetHeadingProperty, value);
    }

    #endregion

    #region Property MapProjection : MapProjection - Проекция координат карты

    public static readonly DependencyProperty MapProjectionProperty = DependencyProperty
       .Register(
            nameof(MapProjection),
            typeof(MapProjection),
            typeof(MapBase),
            new PropertyMetadata(null, (o, _) => ((MapBase)o).MapProjectionPropertyChanged()));

    /// <summary>Проекция координат карты</summary>
    public MapProjection MapProjection
    {
        get => (MapProjection)GetValue(MapProjectionProperty);
        set => SetValue(MapProjectionProperty, value);
    }

    #endregion

    #region Property MapLayer : UIElement - Базовый слой карты

    public static readonly DependencyProperty MapLayerProperty = DependencyProperty
        .Register(
           nameof(MapLayer),
           typeof(UIElement),
           typeof(MapBase),
           new PropertyMetadata(null, (o, e) => ((MapBase)o).MapLayerPropertyChanged((UIElement)e.OldValue, (UIElement)e.NewValue)));

    /// <summary>
    /// Базовый слой карты, который добавляется первым элементов в коллекцию <c>Children</c>.<br/>
    /// Если объект слоя карты реализует интерфейс <see cref="IMapLayer"/> (на пример <see cref="MapTileLayer"/>
    /// или <see cref="MapImageLayer"/>), обладающие значением <see cref="IMapLayer.MapBackground"/>
    /// и <see cref="IMapLayer.MapForeground"/>, то эти свойства используются в качестве
    /// <see cref="Panel.Background"/> and <see cref="MapBase.Foreground"/> значений свойств карты.
    /// </summary>
    public UIElement MapLayer
    {
        get => (UIElement)GetValue(MapLayerProperty);
        set => SetValue(MapLayerProperty, value);
    }

    #endregion

    #region Property ProjectionCenter : Location - Центр азимутальной проекции

    public static readonly DependencyProperty ProjectionCenterProperty = DependencyProperty
       .Register(
            nameof(ProjectionCenter),
            typeof(Location),
            typeof(MapBase),
            new PropertyMetadata(null, (o, _) => ((MapBase)o).ProjectionCenterPropertyChanged()));

    /// <summary>
    /// Опциональный центр азимутальной проекции.
    /// Если <see cref="MapBase.ProjectionCenter"/> <c>is null</c>, то в качестве центра будет использоваться значение свойства <see cref="MapBase.Center"/>.
    /// </summary>
    public Location? ProjectionCenter
    {
        get => (Location?)GetValue(ProjectionCenterProperty);
        set => SetValue(ProjectionCenterProperty, value);
    }

    #endregion

    #region Property MinZoomLevel : double - Минимально допустимый уровень приближения карты

    public static readonly DependencyProperty MinZoomLevelProperty = DependencyProperty
       .Register(
            nameof(MinZoomLevel),
            typeof(double),
            typeof(MapBase),
            new PropertyMetadata(
                1d,
                (o, e) => ((MapBase)o).MinZoomLevelPropertyChanged((double)e.NewValue),
                (o, e) => Math.Max(0, Math.Min((double)e, ((MapBase)o).MaxZoomLevel))),
            v => (double)v >= 0);

    /// <summary>Минимально допустимый уровень приближения карты в интервале [0 .. <see cref="MapBase.MaxZoomLevel"/>]. По умолчанию 1</summary>
    public double MinZoomLevel
    {
        get => (double)GetValue(MinZoomLevelProperty);
        set => SetValue(MinZoomLevelProperty, value);
    }

    #endregion

    #region Property MaxZoomLevel : double - Максимально допустимый уровень приближения карты

    public static readonly DependencyProperty MaxZoomLevelProperty = DependencyProperty
       .Register(
            nameof(MaxZoomLevel),
            typeof(double),
            typeof(MapBase),
            new PropertyMetadata(
                19d,
                (o, e) => ((MapBase)o).MaxZoomLevelPropertyChanged((double)e.NewValue),
                (o, e) => Math.Min(20, Math.Max((double)e, ((MapBase)o).MinZoomLevel))),
            v => (double)v is >= 0 and <= 20);

    /// <summary>Максимально допустимый уровень приближения карты в интервале [<see cref="MapBase.MinZoomLevel"/> ..20]. По умолчанию 19</summary>
    public double MaxZoomLevel
    {
        get => (double)GetValue(MaxZoomLevelProperty);
        set => SetValue(MaxZoomLevelProperty, value);
    }

    #endregion

    #region Property AnimationDuration : TimeSpan - Длительность анимации карты

    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty
       .Register(
            nameof(AnimationDuration),
            typeof(TimeSpan),
            typeof(MapBase),
            new PropertyMetadata(TimeSpan.FromSeconds(0.3)));

    /// <summary>Длительность анимации карты. По умолчанию 0.3 с.</summary>
    public TimeSpan AnimationDuration
    {
        get => (TimeSpan)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    #endregion

    #region Property AnimationEasingFunction : EasingFunctionBase - Функция гладкости анимации

    public static readonly DependencyProperty AnimationEasingFunctionProperty = DependencyProperty
       .Register(
            nameof(AnimationEasingFunction),
            typeof(EasingFunctionBase),
            typeof(MapBase),
            new PropertyMetadata(new QuadraticEase { EasingMode = EasingMode.EaseOut }));

    /// <summary>Функция гладкости анимации. По умолчанию <see cref="QuadraticEase"/> со значением <see cref="EasingMode.EaseOut"/>.</summary>
    public EasingFunctionBase AnimationEasingFunction
    {
        get => (EasingFunctionBase)GetValue(AnimationEasingFunctionProperty);
        set => SetValue(AnimationEasingFunctionProperty, value);
    }

    #endregion

    #region Property TileFadeDuration : TimeSpan - Длительность анимации проявления тайла на карте после его загрузки

    public static readonly DependencyProperty TileFadeDurationProperty = DependencyProperty
       .Register(
            nameof(TileFadeDuration),
            typeof(TimeSpan),
            typeof(MapBase),
            new PropertyMetadata(Tile.FadeDuration, (_, e) => Tile.FadeDuration = (TimeSpan)e.NewValue));

    /// <summary>Длительность анимации проявления тайла на карте после его загрузки. По умолчанию 0.2 с.</summary>
    public TimeSpan TileFadeDuration
    {
        get => (TimeSpan)GetValue(TileFadeDurationProperty);
        set => SetValue(TileFadeDurationProperty, value);
    }

    #endregion

    internal static readonly DependencyProperty CenterPointProperty = DependencyProperty
       .Register(
            "CenterPoint", typeof(Point),
            typeof(MapBase),
            new PropertyMetadata(new Point(), (o, e) => ((MapBase)o).CenterPointPropertyChanged((Point)e.NewValue)));

    public MapProjection LayerMapProjection => (MapLayer as MapTileLayer)?.Projection ?? MapProjection;

    /// <summary>Визуальное графическое преобразование масштабированием элементов карты относительно центра</summary>
    public ScaleTransform ScaleTransform { get; } = new();

    /// <summary>Визуальное графическое преобразование поворотом элементов карты относительно центра</summary>
    public RotateTransform RotateTransform { get; } = new();

    /// <summary>Визуальное графическое преобразование поворотом и масштабированием элементов карты относительно центра</summary>
    public TransformGroup ScaleRotateTransform { get; } = new();


    private ZoomToBoundsCommand? _ZoomToBoundsCommand;

    public ZoomToBoundsCommand ZoomToBoundsCommand => _ZoomToBoundsCommand ??= new(this);

    public MapBase()
    {
        MapProjection = new WebMercatorProjection();
        ScaleRotateTransform.Children.Add(ScaleTransform);
        ScaleRotateTransform.Children.Add(RotateTransform);
    }

    /// <summary>Изменить положение центра карты в системе координат слоя</summary>
    public void TranslateMap(Vector translation) => TranslateMap((Point)translation);

    /// <summary>Изменить центр, угол поворота и масштаб карты</summary>
    public void TransformMap(Point center, Vector translation, double rotation, double scale) => TransformMap(center, (Point)translation, rotation, scale);

    protected override void OnRenderSizeChanged(SizeChangedInfo SizeInfo)
    {
        base.OnRenderSizeChanged(SizeInfo);

        ResetTransformCenter();
        UpdateTransform();
    }

    private PointAnimation? _CenterAnimation;

    private DoubleAnimation? _ZoomLevelAnimation;

    private DoubleAnimation? _HeadingAnimation;

    private Location? _TransformCenter;

    private Point _ViewportCenter;

    private double _CenterLongitude;

    private bool _InternalPropertyChange;

    /// <summary>Преобразование географических координат в экранные</summary>
    public Point LocationToViewportPoint(Location location) => LayerMapProjection.LocationToViewportPoint(location);

    /// <summary>Преобразование экранных координат в географические</summary>
    public Location ViewportPointToLocation(Point point) => LayerMapProjection.ViewportPointToLocation(point);

    /// <summary>
    /// Установка точки временного центра карты в экранных координатах для масштабирования и поворота.<br/>
    /// Данная точка будет автоматически сброшена при при установке свойства <see cref="Center"/> в коде.
    /// </summary>
    public void SetTransformCenter(Point center)
    {
        _TransformCenter = LayerMapProjection.ViewportPointToLocation(center);
        _ViewportCenter = center;
    }

    /// <summary>Сброс временной точки центра графических преобразований, установленных <see cref="SetTransformCenter"/></summary>
    public void ResetTransformCenter()
    {
        _TransformCenter = null;
        _ViewportCenter = new Point(RenderSize.Width / 2d, RenderSize.Height / 2d);
    }

    /// <summary>Изменение положения <see cref="Center"/> в соответствии с установленным преобразованием в экранных координатах</summary>
    public void TranslateMap(Point translation)
    {
        if (_TransformCenter is not null)
        {
            ResetTransformCenter();
            UpdateTransform();
        }

        if (translation is { X: 0, Y: 0 }) return;

        if (Heading != 0d)
        {
            var heading = Heading * Consts.ToRad;
            var cos = Math.Cos(heading);
            var sin = Math.Sin(heading);

            translation = new(
                x: translation.X * cos + translation.Y * sin,
                y: translation.Y * cos - translation.X * sin);
        }

        translation.X = -translation.X;
        translation.Y = -translation.Y;

        Center = LayerMapProjection.TranslateLocation(Center, translation);
    }

    /// <summary>
    /// Изменение центра, угла поворота, и масштаба карты в соответствии с установленными приращениями
    /// перемещения, поворота и масштаба. Поворот и масштаб выполняется относительно выбранной точки центра в экранных координатах.
    /// </summary>
    public void TransformMap(Point center, Point translation, double rotation, double scale)
    {
        if (rotation == 0d && scale == 1d)
        {
            TranslateMap(translation);// more precise
            return;
        }

        _TransformCenter = LayerMapProjection.ViewportPointToLocation(center);
        _ViewportCenter = new Point(center.X + translation.X, center.Y + translation.Y);

        if (rotation != 0d)
        {
            var heading = ((Heading + rotation) % 360d + 360d) % 360d;
            InternalSetValue(HeadingProperty, heading);
            InternalSetValue(TargetHeadingProperty, heading);
        }

        if (scale != 1d)
        {
            var zoom_level = Math.Min(Math.Max(ZoomLevel + Math.Log(scale, 2d), MinZoomLevel), MaxZoomLevel);
            InternalSetValue(ZoomLevelProperty, zoom_level);
            InternalSetValue(TargetZoomLevelProperty, zoom_level);
        }

        UpdateTransform(true);
    }

    /// <summary>Установка значения <see cref="TargetZoomLevel"/> сохраняя заданную центральную точку экранных координат</summary>
    public void ZoomMap(Point center, double ZoomLevel)
    {
        ZoomLevel = Math.Min(Math.Max(ZoomLevel, MinZoomLevel), MaxZoomLevel);

        if (TargetZoomLevel == ZoomLevel) return;

        SetTransformCenter(center);

        if (double.IsNaN(LayerMapProjection.LongitudeScale))
            this.ZoomLevel = ZoomLevel;
        else
            TargetZoomLevel = ZoomLevel;
    }

    /// <summary>
    /// Установка <see cref="TargetZoomLevel"/> и <see cref="TargetCenter"/> в соответствии с определённым значением
    /// интервалов отображаемых координат, вписываемых в текущий экран карты. <see cref="TargetHeading"/> устанавливается в значение 0.
    /// </summary>
    public void ZoomToBounds(BoundingBox BoundingBox)
    {
        if (BoundingBox is not { HasValidBounds: true }) return;

        var projection = LayerMapProjection;
        var rect = projection.BoundingBoxToRect(BoundingBox);
        var center = new Point(rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d);

        var scale0 = 1d / projection.GetViewportScale(0d);

        var (render_width, render_height) = RenderSize;
        var lon_scale = scale0 * render_width / rect.Width;
        var lat_scale = scale0 * render_height / rect.Height;

        var lon_zoom = Math.Log(lon_scale, 2d);
        var lat_zoom = Math.Log(lat_scale, 2d);

        TargetZoomLevel = Math.Min(lon_zoom, lat_zoom);
        TargetCenter = projection.PointToLocation(center);
        TargetHeading = 0d;
    }

    private void MapLayerPropertyChanged(UIElement? OldLayer, UIElement? NewLayer)
    {
        if (OldLayer is not null)
        {
            Children.Remove(OldLayer);

            if (OldLayer is IMapLayer old_map_layer)
            {
                if (old_map_layer.MapBackground is not null) ClearValue(BackgroundProperty);
                if (old_map_layer.MapForeground is not null) ClearValue(ForegroundProperty);
            }
        }

        if (NewLayer is null) return;
        Children.Insert(0, NewLayer);

        if (NewLayer is not IMapLayer new_map_layer) return;

        if (new_map_layer.MapBackground is not null) Background = new_map_layer.MapBackground;
        if (new_map_layer.MapForeground is not null) Foreground = new_map_layer.MapForeground;
    }

    private void MapProjectionPropertyChanged()
    {
        ResetTransformCenter();
        UpdateTransform(false, true);
    }

    private void ProjectionCenterPropertyChanged()
    {
        if (!LayerMapProjection.IsAzimuthal) return;
        ResetTransformCenter();
        UpdateTransform();
    }

    private void AdjustCenterProperty(DependencyProperty property,
#if NET5_0_OR_GREATER
        [NotNullWhen(true)]
#endif 
        ref Location? center)
    {
        if (center is null)
        {
            center = new();
            InternalSetValue(property, center);
        }
        else
        {
            var projection = LayerMapProjection;
            if (center.Longitude is >= -180d and <= 180d && center.Latitude >= -projection.MaxLatitude && center.Latitude <= projection.MaxLatitude)
                return;

            center = new(
                latitude: Math.Min(Math.Max(center.Latitude, -projection.MaxLatitude), projection.MaxLatitude),
                longitude: Location.NormalizeLongitude(center.Longitude));
            InternalSetValue(property, center);
        }
    }

    private void CenterPropertyChanged(Location? center)
    {
        if (_InternalPropertyChange) return;

        AdjustCenterProperty(CenterProperty, ref center);
        UpdateTransform();

        if (_CenterAnimation is not null) return;
        InternalSetValue(TargetCenterProperty, center);
        InternalSetValue(CenterPointProperty, LayerMapProjection.LocationToPoint(center));
        UpdateBounds(ActualWidth, ActualHeight);
    }

    private void UpdateBounds(double Width, double Height)
    {
        if (Width is double.NaN or <= 0) return;
        if (Height is double.NaN or <= 0) return;

        _ = ViewportPointToLocation(new());
        _ = ViewportPointToLocation(new(Width, Height));
    }

    private void TargetCenterPropertyChanged(Location? TargetCenter)
    {
        if (_InternalPropertyChange) return;
        AdjustCenterProperty(TargetCenterProperty, ref TargetCenter);

        if (TargetCenter!.Equals(Center)) return;
        if (_CenterAnimation is not null) _CenterAnimation.Completed -= CenterAnimationCompleted;

        // animate private CenterPoint property by PointAnimation
        var projection = LayerMapProjection;
        _CenterAnimation = new PointAnimation
        {
            From = projection.LocationToPoint(Center),
            To = projection.LocationToPoint(new Location(
                latitude: TargetCenter.Latitude,
                longitude: Location.NearestLongitude(TargetCenter.Longitude, Center.Longitude))),
            Duration = AnimationDuration,
            EasingFunction = AnimationEasingFunction,
            FillBehavior = __AnimationFillBehavior
        };

        _CenterAnimation.Completed += CenterAnimationCompleted;
        BeginAnimation(CenterPointProperty, _CenterAnimation);
    }

    private void CenterAnimationCompleted(object? sender, object e)
    {
        if (_CenterAnimation is null) return;
        _CenterAnimation.Completed -= CenterAnimationCompleted;
        _CenterAnimation = null;

        InternalSetValue(CenterProperty, TargetCenter);
        InternalSetValue(CenterPointProperty, LayerMapProjection.LocationToPoint(TargetCenter));
        UpdateTransform();
    }

    private void CenterPointPropertyChanged(Point CenterPoint)
    {
        if (_InternalPropertyChange) return;
        var center = LayerMapProjection.PointToLocation(CenterPoint);
        center.Longitude = Location.NormalizeLongitude(center.Longitude);

        InternalSetValue(CenterProperty, center);
        UpdateTransform();
    }

    private void MinZoomLevelPropertyChanged(double MinZoomLevel)
    {
        if (MinZoomLevel < 0d || MinZoomLevel > MaxZoomLevel)
        {
            MinZoomLevel = Math.Min(Math.Max(MinZoomLevel, 0d), MaxZoomLevel);
            InternalSetValue(MinZoomLevelProperty, MinZoomLevel);
        }

        if (ZoomLevel < MinZoomLevel)
            ZoomLevel = MinZoomLevel;
    }

    private void MaxZoomLevelPropertyChanged(double MaxZoomLevel)
    {
        if (MaxZoomLevel < MinZoomLevel || MaxZoomLevel > __MaximumZoomLevel)
        {
            MaxZoomLevel = Math.Min(Math.Max(MaxZoomLevel, MinZoomLevel), __MaximumZoomLevel);
            InternalSetValue(MaxZoomLevelProperty, MaxZoomLevel);
        }

        if (ZoomLevel > MaxZoomLevel)
            ZoomLevel = MaxZoomLevel;
    }

    private void AdjustZoomLevelProperty(DependencyProperty property, ref double ZoomLevel)
    {
        if (ZoomLevel >= MinZoomLevel && ZoomLevel <= MaxZoomLevel) return;
        ZoomLevel = Math.Min(Math.Max(ZoomLevel, MinZoomLevel), MaxZoomLevel);
        InternalSetValue(property, ZoomLevel);
    }

    private void ZoomLevelPropertyChanged(double ZoomLevel)
    {
        if (_InternalPropertyChange) return;
        AdjustZoomLevelProperty(ZoomLevelProperty, ref ZoomLevel);
        UpdateTransform();

        if (_ZoomLevelAnimation is null)
            InternalSetValue(TargetZoomLevelProperty, ZoomLevel);
    }

    private void TargetZoomLevelPropertyChanged(double TargetZoomLevel)
    {
        if (_InternalPropertyChange) return;
        AdjustZoomLevelProperty(TargetZoomLevelProperty, ref TargetZoomLevel);

        if (TargetZoomLevel == ZoomLevel) return;
        if (_ZoomLevelAnimation is not null) _ZoomLevelAnimation.Completed -= ZoomLevelAnimationCompleted;

        _ZoomLevelAnimation = new DoubleAnimation
        {
            To = TargetZoomLevel,
            Duration = AnimationDuration,
            EasingFunction = AnimationEasingFunction,
            FillBehavior = __AnimationFillBehavior
        };

        _ZoomLevelAnimation.Completed += ZoomLevelAnimationCompleted;
        BeginAnimation(ZoomLevelProperty, _ZoomLevelAnimation);
    }

    private void ZoomLevelAnimationCompleted(object? sender, object e)
    {
        if (_ZoomLevelAnimation is null) return;
        _ZoomLevelAnimation.Completed -= ZoomLevelAnimationCompleted;
        _ZoomLevelAnimation = null;

        InternalSetValue(ZoomLevelProperty, TargetZoomLevel);
        UpdateTransform(true);
    }

    private void AdjustHeadingProperty(DependencyProperty property, ref double heading)
    {
        if (heading is >= 0d and <= 360d) return;
        heading = (heading % 360d + 360d) % 360d;
        InternalSetValue(property, heading);
    }

    private void HeadingPropertyChanged(double heading)
    {
        if (_InternalPropertyChange) return;
        AdjustHeadingProperty(HeadingProperty, ref heading);
        UpdateTransform();

        if (_HeadingAnimation is null) InternalSetValue(TargetHeadingProperty, heading);
    }

    private void TargetHeadingPropertyChanged(double TargetHeading)
    {
        if (_InternalPropertyChange) return;
        AdjustHeadingProperty(TargetHeadingProperty, ref TargetHeading);

        if (TargetHeading == Heading) return;
        var delta = TargetHeading - Heading;

        if (_HeadingAnimation is not null) _HeadingAnimation.Completed -= HeadingAnimationCompleted;

        _HeadingAnimation = new DoubleAnimation
        {
            By = delta switch
            {
                > 180d => delta - 360d,
                < -180d => delta + 360d,
                _ => delta
            },
            Duration = AnimationDuration,
            EasingFunction = AnimationEasingFunction,
            FillBehavior = __AnimationFillBehavior
        };

        _HeadingAnimation.Completed += HeadingAnimationCompleted;
        BeginAnimation(HeadingProperty, _HeadingAnimation);
    }

    private void HeadingAnimationCompleted(object? sender, object e)
    {
        if (_HeadingAnimation is null) return;
        _HeadingAnimation.Completed -= HeadingAnimationCompleted;
        _HeadingAnimation = null;

        InternalSetValue(HeadingProperty, TargetHeading);
        UpdateTransform();
    }

    private void InternalSetValue(DependencyProperty property, object? value)
    {
        _InternalPropertyChange = true;
        SetValue(property, value);
        _InternalPropertyChange = false;
    }

    private void UpdateTransform(bool ResetTransformCenter = false, bool ProjectionChanged = false)
    {
        var projection = LayerMapProjection;
        var center = _TransformCenter ?? Center;

        projection.SetViewportTransform(ProjectionCenter ?? Center, center, _ViewportCenter, ZoomLevel, Heading);

        if (_TransformCenter is not null)
        {
            center = projection.ViewportPointToLocation(new Point(RenderSize.Width / 2d, RenderSize.Height / 2d));
            center.Longitude = Location.NormalizeLongitude(center.Longitude);

            if (center.Latitude < -projection.MaxLatitude || center.Latitude > projection.MaxLatitude)
            {
                center.Latitude = Math.Min(Math.Max(center.Latitude, -projection.MaxLatitude), projection.MaxLatitude);
                ResetTransformCenter = true;
            }

            InternalSetValue(CenterProperty, center);

            if (_CenterAnimation is null)
            {
                InternalSetValue(TargetCenterProperty, center);
                InternalSetValue(CenterPointProperty, projection.LocationToPoint(center));
            }

            if (ResetTransformCenter)
            {
                this.ResetTransformCenter();
                projection.SetViewportTransform(ProjectionCenter ?? center, center, _ViewportCenter, ZoomLevel, Heading);
            }
        }

        var scale = projection.GetMapScale(center);
        ScaleTransform.ScaleX = scale.X;
        ScaleTransform.ScaleY = scale.Y;
        RotateTransform.Angle = Heading;

        OnViewportChanged(new ViewportChangedEventArgs(ProjectionChanged, Center.Longitude - _CenterLongitude));

        _CenterLongitude = Center.Longitude;
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e)
    {
        base.OnViewportChanged(e);

        ViewportChanged?.Invoke(this, e);
    }
}