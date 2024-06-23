using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MathCore.WPF.Map.Primitives;

/// <summary>Отметка на карте</summary>
public class Pushpin : ContentControl
{
    #region Stroke : Brush - Кисть рамки

    /// <summary>Кисть рамки</summary>
    //[Category("")]
    [Description("Кисть рамки")]
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    /// <summary>Кисть рамки</summary>
    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(Pushpin),
            new FrameworkPropertyMetadata(
                null, 
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

    #endregion

    public Pushpin()
    {
        DefaultStyleKey = typeof(Pushpin);

        MapPanel.InitMapElement(this);
    }
}