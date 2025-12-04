using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MathCore.WPF.Map;

/// <summary>Базовый класс объектов, размещаемых на поверхности карты</summary>
public class MapOverlay : MapPanel
{
    #region Property FontSize : double

    public static readonly DependencyProperty FontSizeProperty = Control.FontSizeProperty.AddOwner(typeof(MapOverlay));

    /// <summary>Размер шрифта для текстовых элементов оверлея</summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    #endregion

    #region Property FontFamily : FontFamily

    public static readonly DependencyProperty FontFamilyProperty = Control.FontFamilyProperty.AddOwner(typeof(MapOverlay));

    /// <summary>Семейство шрифтов для текстовых элементов оверлея</summary>
    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    #endregion

    #region Property FontStyle : FontStyle

    public static readonly DependencyProperty FontStyleProperty = Control.FontStyleProperty.AddOwner(typeof(MapOverlay));

    /// <summary>Стиль шрифта для текстовых элементов оверлея</summary>
    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    #endregion

    #region Property FontStretch : FontStretch

    public static readonly DependencyProperty FontStretchProperty = Control.FontStretchProperty.AddOwner(typeof(MapOverlay));

    /// <summary>Растяжение шрифта для текстовых элементов оверлея</summary>
    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    #endregion

    #region Property FontWeight : FontWeight

    public static readonly DependencyProperty FontWeightProperty = Control.FontWeightProperty.AddOwner(typeof(MapOverlay));

    /// <summary>Начертание шрифта для текстовых элементов оверлея</summary>
    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    #endregion

    #region Property Foreground : Brush

    public static readonly DependencyProperty ForegroundProperty = Control.ForegroundProperty.AddOwner(typeof(MapOverlay));

    /// <summary>Основная кисть рисования элементов оверлея</summary>
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    #endregion

    #region Property Stroke : Brush

    public static readonly DependencyProperty StrokeProperty = Shape.StrokeProperty.AddOwner(
        typeof(MapOverlay), 
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Кисть контура для фигур оверлея</summary>
    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }
    #endregion

    #region Property StrokeThickness : double

    public static readonly DependencyProperty StrokeThicknessProperty = Shape.StrokeThicknessProperty.AddOwner(
        typeof(MapOverlay),
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Толщина контура фигур оверлея</summary>
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    #endregion

    #region Property StrokeDashArray : DoubleCollection

    public static readonly DependencyProperty StrokeDashArrayProperty = Shape.StrokeDashArrayProperty.AddOwner(
        typeof(MapOverlay),
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Шаблон штриховки контура фигур оверлея</summary>
    public DoubleCollection StrokeDashArray
    {
        get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    #endregion

    #region Property StrokeDashOffset : double

    public static readonly DependencyProperty StrokeDashOffsetProperty = Shape.StrokeDashOffsetProperty.AddOwner(
        typeof(MapOverlay),
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Смещение шаблона штриховки контура</summary>
    public double StrokeDashOffset
    {
        get => (double)GetValue(StrokeDashOffsetProperty);
        set => SetValue(StrokeDashOffsetProperty, value);
    }

    #endregion

    #region Property StrokeDashCap : PenLineCap

    public static readonly DependencyProperty StrokeDashCapProperty = Shape.StrokeDashCapProperty.AddOwner(
        typeof(MapOverlay), 
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Тип окончания штрихов контура</summary>
    public PenLineCap StrokeDashCap
    {
        get => (PenLineCap)GetValue(StrokeDashCapProperty);
        set => SetValue(StrokeDashCapProperty, value);
    }

    #endregion

    #region Property StrokeStartLineCap : PenLineCap

    public static readonly DependencyProperty StrokeStartLineCapProperty = Shape.StrokeStartLineCapProperty.AddOwner(
        typeof(MapOverlay), 
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Тип окончания контура в начале линии</summary>
    public PenLineCap StrokeStartLineCap
    {
        get => (PenLineCap)GetValue(StrokeStartLineCapProperty);
        set => SetValue(StrokeStartLineCapProperty, value);
    }

    #endregion

    #region Property StrokeEndLineCap : PenLineCap

    public static readonly DependencyProperty StrokeEndLineCapProperty = Shape.StrokeEndLineCapProperty.AddOwner(
        typeof(MapOverlay), 
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Тип окончания контура в конце линии</summary>
    public PenLineCap StrokeEndLineCap
    {
        get => (PenLineCap)GetValue(StrokeEndLineCapProperty);
        set => SetValue(StrokeEndLineCapProperty, value);
    }

    #endregion

    #region Property StrokeLineJoin : PenLineJoin

    public static readonly DependencyProperty StrokeLineJoinProperty = Shape.StrokeLineJoinProperty.AddOwner(
        typeof(MapOverlay), 
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Тип соединения сегментов контура</summary>
    public PenLineJoin StrokeLineJoin
    {
        get => (PenLineJoin)GetValue(StrokeLineJoinProperty);
        set => SetValue(StrokeLineJoinProperty, value);
    }

    #endregion

    #region Property StrokeMiterLimit : double

    public static readonly DependencyProperty StrokeMiterLimitProperty = Shape.StrokeMiterLimitProperty.AddOwner(
        typeof(MapOverlay), 
        new FrameworkPropertyMetadata { AffectsRender = true });

    /// <summary>Предел скоса для соединений типа Miter</summary>
    public double StrokeMiterLimit
    {
        get => (double)GetValue(StrokeMiterLimitProperty);
        set => SetValue(StrokeMiterLimitProperty, value);
    } 

    #endregion

    /// <summary>Устанавливает карту‑владельца и настраивает привязки кистей</summary>
    /// <param name="ParentMap">Экземпляр карты</param>
    protected override void SetParentMap(MapBase? ParentMap)
    {
        if (GetBindingExpression(StrokeProperty) is not null) 
            ClearValue(StrokeProperty);

        if (ParentMap is not null && Stroke is null)
            this.SetBinding(StrokeProperty, nameof(Foreground), ParentMap);

        base.SetParentMap(ParentMap);
    }
}