using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map;

/// <summary>Панель элементов карты</summary>
/// <remarks>
/// Размещает дочерние элементы на <see cref="Map"/> в позициях, указанных прикрепленным свойством
/// <see cref="MapPanel"/>.<see cref="MapPanel.LocationProperty"/>, или в прямоугольных областях,
/// указанных прикрепленным свойством <see cref="MapPanel"/>.<see cref="MapPanel.BoundingBoxProperty"/>.
/// Позиция экрана элемента определяется по средствам преобразования <see cref="TranslateTransform"/> его свойства
/// <see cref="UIElement.RenderTransform"/> либо напрямую, либо последнего дочернему элементу <see cref="TransformGroup"/>.
/// </remarks>
public class MapPanel : Panel, IMapElement
{
    public static readonly DependencyProperty LocationProperty = DependencyProperty
       .RegisterAttached(
            "Location", 
            typeof(Location), 
            typeof(MapPanel), 
            new PropertyMetadata(null, LocationPropertyChanged));

    public static readonly DependencyProperty BoundingBoxProperty = DependencyProperty
       .RegisterAttached(
            "BoundingBox", 
            typeof(BoundingBox), 
            typeof(MapPanel), 
            new PropertyMetadata(null, BoundingBoxPropertyChanged));

    #region Attached property ParentMap : MapBase

    private static readonly DependencyPropertyKey __ParentMapPropertyKey = DependencyProperty
        .RegisterAttachedReadOnly(
             nameof(ParentMap),
             typeof(MapBase),
             typeof(MapPanel),
             new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, ParentMapPropertyChanged));

    public static readonly DependencyProperty ParentMapProperty = __ParentMapPropertyKey.DependencyProperty;

    public static MapBase GetParentMap(UIElement element) => (MapBase)element.GetValue(ParentMapProperty);

    public static void InitMapElement(FrameworkElement element) => (element as MapBase)?.SetValue(__ParentMapPropertyKey, element); 

    #endregion

    private MapBase _ParentMap;

    public MapPanel() => InitMapElement(this);

    public static Location GetLocation(UIElement element) => (Location)element.GetValue(LocationProperty);

    public static void SetLocation(UIElement element, Location value) => element.SetValue(LocationProperty, value);

    public static BoundingBox GetBoundingBox(UIElement element) => (BoundingBox)element.GetValue(BoundingBoxProperty);

    public static void SetBoundingBox(UIElement element, BoundingBox value) => element.SetValue(BoundingBoxProperty, value);

    public MapBase ParentMap
    {
        get => _ParentMap;
        set => SetParentMap(value);
    }

    protected override Size MeasureOverride(Size AvailableSize)
    {
        AvailableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (UIElement element in Children) 
            element.Measure(AvailableSize);

        return new Size();
    }

    protected override Size ArrangeOverride(Size FinalSize)
    {
        foreach (UIElement element in Children)
            if (GetBoundingBox(element) is { } bounding_box)
            {
                ArrangeElementWithBoundingBox(element);
                SetBoundingBoxRect(element, _ParentMap, bounding_box);
            }
            else if (GetLocation(element) is { } location)
            {
                ArrangeElementWithLocation(element);
                SetViewportPosition(element, _ParentMap, location);
            }
            else
                ArrangeElement(element, FinalSize);

        return FinalSize;
    }

    protected virtual void SetParentMap(MapBase ParentMap)
    {
        if (_ParentMap is not null && _ParentMap != this) 
            _ParentMap.ViewportChanged -= OnViewportChanged;

        _ParentMap = ParentMap;

        if (_ParentMap is null || ReferenceEquals(_ParentMap, this)) return;

        _ParentMap.ViewportChanged += OnViewportChanged;
        OnViewportChanged(new ViewportChangedEventArgs());
    }

    protected virtual void OnViewportChanged(ViewportChangedEventArgs e)
    {
        foreach (UIElement element in Children)
        {
            BoundingBox bounding_box;
            Location location;

            if ((bounding_box = GetBoundingBox(element)) is not null)
                SetBoundingBoxRect(element, _ParentMap, bounding_box);
            else if ((location = GetLocation(element)) is not null) 
                SetViewportPosition(element, _ParentMap, location);
        }
    }

    private void OnViewportChanged(object sender, ViewportChangedEventArgs e) => OnViewportChanged(e);

    private static void ParentMapPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is IMapElement map_element) 
            map_element.ParentMap = e.NewValue as MapBase;
    }

    private static void LocationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var element = (UIElement)obj;
        var map = GetParentMap(element);
        var location = (Location)e.NewValue;

        if (location is null)
            ArrangeElement(element, map?.RenderSize ?? new());
        else if (e.OldValue is null) 
            ArrangeElementWithLocation(element); // размещается единожды, после этого Location будет null

        SetViewportPosition(element, map, location);
    }

    private static void BoundingBoxPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var element = (FrameworkElement)obj;
        var map = GetParentMap(element);
        var bounding_box = (BoundingBox)e.NewValue;

        if (bounding_box is null)
            ArrangeElement(element, map?.RenderSize ?? new());
        else if (e.OldValue is null) 
            ArrangeElementWithBoundingBox(element); // размещается единожды, после этого Location будет null

        SetBoundingBoxRect(element, map, bounding_box);
    }

    private static void SetViewportPosition(UIElement element, MapBase ParentMap, Location location)
    {
        var viewport_position = new Point();

        if (ParentMap is
            {
                LayerMapProjection: var projection, 
                RenderSize:
                {
                    Width: var width, 
                    Height: var height
                }, 
                Center.Longitude: var parent_center_longitude
            } 
            && location is { Latitude: var latitude, Longitude: var longitude })
        {
            viewport_position = projection.LocationToViewportPoint(location);

            if (viewport_position.X < 0d || viewport_position.X > width ||
                viewport_position.Y < 0d || viewport_position.Y > height)
                viewport_position = projection.LocationToViewportPoint(new(
                    latitude: latitude,
                    longitude: Location.NearestLongitude(longitude, parent_center_longitude)));

            if ((bool)element.GetValue(UseLayoutRoundingProperty))
            {
                viewport_position.X = Math.Round(viewport_position.X);
                viewport_position.Y = Math.Round(viewport_position.Y);
            }
        }

        var translate_transform = element.RenderTransform as TranslateTransform;

        if (translate_transform is null)
        {
            if (element.RenderTransform is not TransformGroup transform_group)
            {
                translate_transform = new();
                element.RenderTransform = translate_transform;
            }
            else
            {
                if (transform_group.Children.Count > 0)
                    translate_transform = transform_group.Children[^1] as TranslateTransform;

                if (translate_transform is null)
                {
                    translate_transform = new();
                    transform_group.Children.Add(translate_transform);
                }
            }
        }

        translate_transform.X = viewport_position.X;
        translate_transform.Y = viewport_position.Y;
    }

    private static void SetBoundingBoxRect(UIElement element, MapBase ParentMap, BoundingBox BoundingBox)
    {
        var rotation = 0d;
        var viewport_position = new Point();

        if (ParentMap is
            {
                LayerMapProjection:
                {
                    ViewportTransform: var viewport_transform, 
                    ViewportScale: var view_port_scale
                } projection,
                Heading: var heading,
                RenderSize:
                {
                    Width: var render_size_width,
                    Height: var render_size_height
                }
            } 
            && BoundingBox is not null)
        {
            var rect = projection.BoundingBoxToRect(BoundingBox);
            var center = new Point(rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d);
            
            rotation = heading;
            viewport_position = viewport_transform.Transform(center);

            if (viewport_position.X < 0d || viewport_position.X > render_size_width ||
                viewport_position.Y < 0d || viewport_position.Y > render_size_height)
            {
                var location = projection.PointToLocation(center);
                location.Longitude = Location.NearestLongitude(location.Longitude, ParentMap.Center.Longitude);

                viewport_position = projection.LocationToViewportPoint(location);
            }

            var width = rect.Width * view_port_scale;
            var height = rect.Height * view_port_scale;

            if (element is not FrameworkElement framework_element)
                element.Arrange(new Rect(-width / 2d, -height / 2d, width, height));
            else
            {
                framework_element.Width = width;
                framework_element.Height = height;
            }
        }


        if (element.RenderTransform is not TransformGroup transform_group ||
            transform_group.Children.Count != 2 ||
            transform_group.Children[0] is not RotateTransform rotate_transform ||
            transform_group.Children[1] is not TranslateTransform translate_transform)
        {
            transform_group = new();
            rotate_transform = new();
            translate_transform = new();
            transform_group.Children.Add(rotate_transform);
            transform_group.Children.Add(translate_transform);

            element.RenderTransform = transform_group;
            element.RenderTransformOrigin = new(0.5, 0.5);
        }

        rotate_transform.Angle = rotation;
        translate_transform.X = viewport_position.X;
        translate_transform.Y = viewport_position.Y;
    }

    private static void ArrangeElementWithBoundingBox(UIElement element)
    {
        var size = element.DesiredSize;

        element.Arrange(new(-size.Width / 2d, -size.Height / 2d, size.Width, size.Height));
    }

    private static void ArrangeElementWithLocation(UIElement element)
    {
        if (element is not FrameworkElement { HorizontalAlignment: var horizontal_alignment, VerticalAlignment: var vertical_alignment })
        {
            element.Arrange(new(element.DesiredSize));
            return;
        }

        var rect = new Rect(element.DesiredSize);
        rect.X = horizontal_alignment switch
        {
            HorizontalAlignment.Center => -rect.Width / 2d,
            HorizontalAlignment.Right => -rect.Width,
            _ => rect.X
        };

        rect.Y = vertical_alignment switch
        {
            VerticalAlignment.Center => -rect.Height / 2d,
            VerticalAlignment.Bottom => -rect.Height,
            _ => rect.Y
        };

        element.Arrange(rect);
    }

    private static void ArrangeElement(UIElement element, Size ParentSize)
    {
        if (element is not FrameworkElement { HorizontalAlignment: var horizontal_alignment, VerticalAlignment: var vertical_alignment })
        {
            element.Arrange(new Rect(element.DesiredSize));
            return;
        }

        var (width, height) = element.DesiredSize;
        var (parent_width, parent_height) = ParentSize;

        var rect = new Rect(element.DesiredSize);
        switch (horizontal_alignment)
        {
            case HorizontalAlignment.Center:
                rect.X = (parent_width - width) / 2d;
                break;

            case HorizontalAlignment.Right:
                rect.X = parent_width - width;
                break;

            case HorizontalAlignment.Stretch:
                rect.Width = parent_width;
                break;
        }

        switch (vertical_alignment)
        {
            case VerticalAlignment.Center:
                rect.Y = (parent_height - height) / 2d;
                break;

            case VerticalAlignment.Bottom:
                rect.Y = parent_height - height;
                break;

            case VerticalAlignment.Stretch:
                rect.Height = parent_height;
                break;
        }

        element.Arrange(rect);
    }
}