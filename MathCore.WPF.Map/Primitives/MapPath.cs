using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map.Primitives;

/// <summary>Последовательность точек на карте, соединённых линией</summary>
public class MapPath : Shape, IMapElement
{
    static MapPath() => StretchProperty.OverrideMetadata(typeof(MapPath), new FrameworkPropertyMetadata { CoerceValueCallback = (_, _) => Stretch.None });

    #region property Data : Geometry - геометрия

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data),
        typeof(Geometry),
        typeof(MapPath),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsRender,
            DataPropertyChanged,
            CoerceDataProperty));

    public Geometry Data
    {
        get => (Geometry)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static void DataPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.OldValue, e.NewValue)) return;
        var map_path = (MapPath)obj;

        if (e.OldValue is Geometry old_geometry)
            old_geometry.ClearValue(Geometry.TransformProperty);

        if (e.NewValue is Geometry new_geometry)
            new_geometry.Transform = map_path._ViewportTransform;
    }

    private static object CoerceDataProperty(DependencyObject obj, object value) =>
        value is Geometry { IsFrozen: true } data
            ? data.CloneCurrentValue()
            : value;

    #endregion

    #region Property Location : Location - точка размещения геометрии на карте

    public static readonly DependencyProperty LocationProperty = DependencyProperty
       .Register(
            nameof(Location),
            typeof(Location),
            typeof(MapPath),
            new PropertyMetadata(null, (o, _) => ((MapPath)o).LocationPropertyChanged()));

    public Location Location
    {
        get => (Location)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    #endregion

    private MapBase? _ParentMap;

    private readonly TransformGroup _ViewportTransform = new();

    protected override Geometry DefiningGeometry => Data;

    public MapBase? ParentMap
    {
        get => _ParentMap;
        set
        {
            if (_ParentMap is not null) _ParentMap.ViewportChanged -= OnViewportChanged;

            _ViewportTransform.Children.Clear();
            _ParentMap = value;

            if (_ParentMap is { LayerMapProjection.ViewportTransform: var projection })
            {
                _ViewportTransform.Children.Add(new TranslateTransform());
                _ViewportTransform.Children.Add(projection);
                _ParentMap.ViewportChanged += OnViewportChanged;
            }

            UpdateData();
        }
    }

    public MapPath() => MapPanel.InitMapElement(this);

    protected virtual void UpdateData() { }

    protected virtual void OnViewportChanged(ViewportChangedEventArgs e)
    {
        if (_ParentMap is not
            {
                LayerMapProjection:
                {
                    LongitudeScale: var longitude_scale,
                    ViewportTransform: var viewport_transform
                } projection,
                Center: { Longitude: var center_longitude },
                RenderSize:
                {
                    Width: var render_width,
                    Height: var render_height
                }
            })
            return;

        if (e.ProjectionChanged)
            _ViewportTransform.Children[1] = viewport_transform;

        if (e.ProjectionChanged || double.IsNaN(longitude_scale))
            UpdateData();

        if (double.IsNaN(longitude_scale))
            return;

        var longitude_offset = 0d;

        if (Location is { Longitude: var longitude } location)
        {
            var viewport_position = projection.LocationToViewportPoint(location);

            if (viewport_position.X < 0d || viewport_position.X > render_width ||
                viewport_position.Y < 0d || viewport_position.Y > render_height)
            {
                var nearest_longitude = Location.NearestLongitude(longitude, center_longitude);

                longitude_offset = longitude_scale * (nearest_longitude - longitude);
            }
        }

        ((TranslateTransform)_ViewportTransform.Children[0]).X = longitude_offset;
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e) => OnViewportChanged(e);

    private void LocationPropertyChanged()
    {
        if (_ParentMap is not null)
            OnViewportChanged(new());
    }

    protected override Size MeasureOverride(Size AvailableSize) => new();
}