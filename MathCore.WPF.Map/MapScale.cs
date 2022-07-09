using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MathCore.WPF.Map;

/// <summary>Сетка шкалы карты</summary>
public class MapScale : MapOverlay
{
    #region Property Padding : Thickness

    public static readonly DependencyProperty PaddingProperty = DependencyProperty
      .Register(
           nameof(Padding),
           typeof(Thickness),
           typeof(MapScale),
           null);

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    } 

    #endregion

    private readonly TextBlock _Label = new();

    private readonly Polyline _Line = new();

    public MapScale()
    {
        IsHitTestVisible = false;
        MinWidth = 100d;
        Padding = new Thickness(4d);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Bottom;

        _Label.HorizontalAlignment = HorizontalAlignment.Left;
        _Label.VerticalAlignment = VerticalAlignment.Top;
        _Label.TextAlignment = TextAlignment.Center;

        _Label.SetBinding(TextBlock.ForegroundProperty,
            GetBindingExpression(ForegroundProperty)?.ParentBinding ??
            new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Foreground))
            });

        _Line.SetBinding(Shape.StrokeProperty,
            GetBindingExpression(StrokeProperty)?.ParentBinding ??
            new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Stroke))
            });

        _Line.SetBinding(Shape.StrokeThicknessProperty,
            GetBindingExpression(StrokeThicknessProperty)?.ParentBinding ??
            new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(StrokeThickness))
            });

        Children.Add(_Line);
        Children.Add(_Label);
    }

    protected override Size MeasureOverride(Size AvailableSize)
    {
        var size = new Size();

        if (ParentMap is null || ParentMap.ScaleTransform.ScaleX <= 0d) 
            return size;

        var length = MinWidth / ParentMap.ScaleTransform.ScaleX;
        var magnitude = Math.Pow(10d, Math.Floor(Math.Log10(length)));

        length = (length / magnitude) switch
        {
            < 2d => 2d * magnitude,
            < 5d => 5d * magnitude,
            _ => 10d * magnitude
        };

        size.Width = length * ParentMap.ScaleTransform.ScaleX + StrokeThickness + Padding.Left + Padding.Right;
        size.Height = 1.25 * FontSize + StrokeThickness + Padding.Top + Padding.Bottom;

        var x1 = Padding.Left + StrokeThickness / 2d;
        var x2 = size.Width - Padding.Right - StrokeThickness / 2d;
        var y1 = size.Height / 2d;
        var y2 = size.Height - Padding.Bottom - StrokeThickness / 2d;
        var points = new PointCollection();
        points.Add(new Point(x1, y1));
        points.Add(new Point(x1, y2));
        points.Add(new Point(x2, y2));
        points.Add(new Point(x2, y1));

        _Line.Points = points;
        _Line.Measure(size);

        if (FontFamily is not null) 
            _Label.FontFamily = FontFamily;

        _Label.FontSize = FontSize;
        _Label.FontStyle = FontStyle;
        _Label.FontStretch = FontStretch;
        _Label.FontWeight = FontWeight;
        _Label.Text = length >= 1000d ? $"{length / 1000d:0} km" : $"{length:0} m";
        _Label.Width = size.Width;
        _Label.Height = size.Height;
        _Label.Measure(size);

        return size;
    }

    protected override void OnViewportChanged(ViewportChangedEventArgs e) => InvalidateMeasure();
}