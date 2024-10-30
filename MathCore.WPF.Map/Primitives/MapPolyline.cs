using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map.Primitives;

/// <summary>Полигон из точек на карте</summary>
public class MapPolyline : MapPath
{
    #region Property FillRule : FillRule - Способ заливки полигона

    public static readonly DependencyProperty FillRuleProperty = DependencyProperty
         .Register(
              nameof(FillRule),
              typeof(FillRule),
              typeof(MapPolyline),
              new FrameworkPropertyMetadata(
                  FillRule.EvenOdd,
                  FrameworkPropertyMetadataOptions.AffectsRender,
                  (o, e) => ((StreamGeometry)((MapPolyline)o).Data).FillRule = (FillRule)e.NewValue));

    /// <summary>Способ заливки полигона</summary>
    public FillRule FillRule
    {
        get => (FillRule)GetValue(FillRuleProperty);
        set => SetValue(FillRuleProperty, value);
    }

    #endregion

    #region Property Locations : IEnumerable<Location> - Набор точек полигона

    public static readonly DependencyProperty LocationsProperty = DependencyProperty
        .Register(
             nameof(Locations),
             typeof(IEnumerable<Location>),
             typeof(MapPolyline),
             new(null, (o, e) => ((MapPolyline)o).LocationsPropertyChanged(e)));

    /// <summary>Набор точек полигона</summary>
    [TypeConverter(typeof(LocationCollectionConverter))]
    public IEnumerable<Location>? Locations
    {
        get => (IEnumerable<Location>?)GetValue(LocationsProperty);
        set => SetValue(LocationsProperty, value);
    }

    #endregion

    #region Property IsClosed : bool - Является ли периметр полигона замкнутым

    public static readonly DependencyProperty IsClosedProperty = DependencyProperty
        .Register(
             nameof(IsClosed),
             typeof(bool),
             typeof(MapPolyline),
             new(false, (o, _) => ((MapPolyline)o).UpdateData()));

    /// <summary>Является ли периметр полигона замкнутым</summary>
    public bool IsClosed
    {
        get => (bool)GetValue(IsClosedProperty);
        set => SetValue(IsClosedProperty, value);
    } 

    #endregion

    public MapPolyline() => Data = new StreamGeometry();

    protected override void UpdateData()
    {
        var geometry = (StreamGeometry)Data;

        var locations = Locations;
        if (ParentMap is null || locations is null)
        {
            geometry.Clear();
            return;
        }

        var points = locations.Select(ParentMap.LayerMapProjection.LocationToPoint).ToList();
        if (points.Count < 2)
        {
            geometry.Clear();
            return;
        }

        using var context = geometry.Open();

        context.BeginFigure(points[0], IsClosed, IsClosed);
        points.RemoveAt(0);
        context.PolyLineTo(points, true, false);
    }

    private void LocationsPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        var new_collection = e.NewValue as INotifyCollectionChanged;

        if (e.OldValue is INotifyCollectionChanged old_collection) 
            old_collection.CollectionChanged -= LocationCollectionChanged;

        if (new_collection is not null)
            new_collection.CollectionChanged += LocationCollectionChanged;

        UpdateData();
    }

    private void LocationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateData();
}