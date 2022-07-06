using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map;

/// <summary>Сетка карты</summary>
public class MapGraticule : MapOverlay
{
    static MapGraticule()
    {
        IsHitTestVisibleProperty.OverrideMetadata(typeof(MapGraticule), new FrameworkPropertyMetadata(false));
        StrokeThicknessProperty.OverrideMetadata(typeof(MapGraticule), new FrameworkPropertyMetadata(0.5));
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e) => InvalidateVisual();

    protected override void OnRender(DrawingContext DrawingContext)
    {
        if (ParentMap is not { LayerMapProjection: var projection, RenderSize: var render_size } || 
            double.IsNaN(projection.LongitudeScale))
            return;

        var bounds = projection.ViewportRectToBoundingBox(new(render_size));
        var line_distance = GetLineDistance();
        var label_format = GetLabelFormat(line_distance);

        var stroke_thickness = StrokeThickness;
        var pen = new Pen
        {
            Brush = Stroke,
            Thickness = stroke_thickness,
            DashStyle = new(StrokeDashArray, StrokeDashOffset),
            DashCap = StrokeDashCap
        };

        var lat_label_start = Math.Ceiling(bounds.South / line_distance) * line_distance;
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var lat_labels = new List<(double Position, FormattedText Text)>((int)((bounds.North - lat_label_start) / line_distance) + 1);
        var font_size = FontSize;
        var foreground = Foreground;
        for (var lat = lat_label_start; lat <= bounds.North; lat += line_distance)
        {
            lat_labels.Add((lat, new(
                 textToFormat: GetLabelText(lat, label_format, "NS"),
                      culture: CultureInfo.InvariantCulture,
                flowDirection: FlowDirection.LeftToRight,
                     typeface: typeface,
                       emSize: font_size,
                   foreground: foreground, 
                 pixelsPerDip: 92)));

            DrawingContext.DrawLine(pen,
                projection.LocationToViewportPoint(new(lat, bounds.West)),
                projection.LocationToViewportPoint(new(lat, bounds.East)));
        }

        var lon_label_start = Math.Ceiling(bounds.West / line_distance) * line_distance;
        var lon_labels = new List<(double Position, FormattedText Text)>((int)((bounds.East - lon_label_start) / line_distance) + 1);
        var culture = CultureInfo.InvariantCulture;
        for (var lon = lon_label_start; lon <= bounds.East; lon += line_distance)
        {
            lon_labels.Add((lon, new(
                 textToFormat: GetLabelText(Location.NormalizeLongitude(lon), label_format, "EW"),
                      culture: culture,
                flowDirection: FlowDirection.LeftToRight,
                     typeface: typeface,
                       emSize: font_size,
                   foreground: foreground,
                 pixelsPerDip: 92)));

            DrawingContext.DrawLine(pen,
                projection.LocationToViewportPoint(new(bounds.South, lon)),
                projection.LocationToViewportPoint(new(bounds.North, lon)));
        }

        var map_heading = ParentMap.Heading;
        var stroke_thickness_half = stroke_thickness * 0.5;
        foreach (var (lat_pos, lat_text) in lat_labels)
            foreach (var (lon_pos, lon_text) in lon_labels)
            {
                var position = projection.LocationToViewportPoint(new(lat_pos, lon_pos));

                DrawingContext.PushTransform(new RotateTransform(map_heading, position.X, position.Y));
                DrawingContext.DrawText(
                    lat_text,
                    new(position.X + stroke_thickness_half + 2d, position.Y - stroke_thickness_half - lat_text.Height));
                DrawingContext.DrawText(
                    lon_text,
                    new(position.X + stroke_thickness_half + 2d, position.Y + stroke_thickness_half));
                DrawingContext.Pop();
            }
    }

    public static readonly DependencyProperty MinLineDistanceProperty = DependencyProperty
       .Register(
            nameof(MinLineDistance),
            typeof(double),
            typeof(MapGraticule),
            new PropertyMetadata(150d));

    private static readonly double[] __LineDistances = { 1, 2, 5, 10, 15, 30, 60 };

    /// <summary>Минимальный шаг сетки в пикселях. По умолчанию имеет значение 150.</summary>
    [Description("Минимальный шаг сетки в пикселях")]
    public double MinLineDistance
    {
        get => (double)GetValue(MinLineDistanceProperty);
        set => SetValue(MinLineDistanceProperty, value);
    }

    private double GetLineDistance()
    {
        var min_distance = MinLineDistance / MapProjection.DegreesToViewportScale(ParentMap.ZoomLevel);
        var scale = 1d;

        if (min_distance < 1d)
        {
            scale = min_distance < __MinDistance ? 3600d : 60d;
            min_distance *= scale;
        }

        var i = 0;

        var distances = __LineDistances;
        var distances_length = distances.Length - 1;
        while (i < distances_length && distances[i] < min_distance)
            i++;

        return distances[i] / scale;
    }

    private const double __MinDistance = 1 / 60d;
    private static string GetLabelFormat(double LineDistance) => LineDistance switch
    {
        < __MinDistance => "{0} {1}°{2:00}'{3:00}\"",
        < 1d => "{0} {1}°{2:00}'",
        _ => "{0} {1}°"
    };

    private static string GetLabelText(double value, string format, string hemispheres)
    {
        var hemisphere = hemispheres[0];

        if (value < -1e-8) // ~1мм
        {
            value = -value;
            hemisphere = hemispheres[1];
        }

        var seconds = (int)Math.Round(value * 3600d);

        return string.Format(format, hemisphere, seconds / 3600, seconds / 60 % 60, seconds % 60);
    }
}