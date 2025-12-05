using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MathCore.WPF.Map.Converters;

/// <summary>Преобразователь уровней масштабирования карты</summary>
/// <remarks>Конвертер используется в RenterTranszorm:ScaleTransform для визуального масштабирования элемента согласно текущему масштабу карты</remarks>
public class Zoom : Freezable, IValueConverter
{
    #region Property MaxZoom : double - Максимально допустимый уровень масштаба карты 

    public static readonly DependencyProperty MaxZoomProperty =
        DependencyProperty.Register(
            nameof(MaxZoom),
            typeof(double),
            typeof(Map),
            new(double.NaN, null, OnCoerceMaxZoom), OnValidateMaxZoom);

    private static object OnCoerceMaxZoom(DependencyObject d, object base_value)
    {
        if (base_value is not double zoom || double.IsNaN(zoom))
            return double.NaN;
        if (d.GetValue(MinZoomProperty) is double min_zoom && !double.IsNaN(min_zoom) && zoom < min_zoom)
            return min_zoom;
        return zoom;
    }

    private static bool OnValidateMaxZoom(object value)
    {
        if (value is not double zoom || double.IsNaN(zoom))
            return true;
        return zoom >= 0;
    }

    /// <summary>Максимально допустимый уровень масштаба карты</summary>
    /// <remarks>Чем значение меньше, тем уровень масштаба больше. То есть 1 - это больший уровень масштаба, чем 10</remarks>
    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    #endregion

    #region Property MinZoom : double - Минимально допустимый уровень масштаба карты

    public static readonly DependencyProperty MinZoomProperty =
        DependencyProperty.Register(
            nameof(MinZoom),
            typeof(double),
            typeof(Map),
            new(double.NaN, null, OnCoerceMinZoom), OnValidateMinZoom);

    private static object OnCoerceMinZoom(DependencyObject d, object base_value)
    {
        if (base_value is not double zoom || double.IsNaN(zoom))
            return double.NaN;
        if (d.GetValue(MaxZoomProperty) is double max_zoom && !double.IsNaN(max_zoom) && zoom > max_zoom)
            return max_zoom;
        return zoom;
    }

    private static bool OnValidateMinZoom(object value)
    {
        if (value is not double zoom || double.IsNaN(zoom))
            return true;
        return zoom >= 0;
    }

    /// <summary>Минимально допустимый уровень масштаба карты</summary>
    /// <remarks>Чем значение больше, тем уровень масштаба меньше: 10 уровень масштаба более мелкий, чем уровень 1</remarks>
    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    #endregion

    #region Property CurrentZoom : double - Текущий выбранный урвоень масштаба карты

    public static readonly DependencyProperty CurrentZoomProperty =
        DependencyProperty.Register(
            nameof(CurrentZoom),
            typeof(double),
            typeof(Map),
            new(double.NaN, null, OnCoerceCurrentZoom), OnValidateCurrentZoom);

    private static object OnCoerceCurrentZoom(DependencyObject d, object base_value)
    {
        if (base_value is not double zoom || double.IsNaN(zoom))
            return double.NaN;

        var min_zoom = (double)d.GetValue(MinZoomProperty);
        if (!double.IsNaN(min_zoom) && zoom < min_zoom)
            return min_zoom;

        var max_zoom = (double)d.GetValue(MaxZoomProperty);
        if (!double.IsNaN(max_zoom) && zoom > max_zoom)
            return max_zoom;

        return zoom;
    }

    private static bool OnValidateCurrentZoom(object value)
    {
        if (value is not double zoom || double.IsNaN(zoom))
            return true;
        return zoom >= 0;
    }

    /// <summary>Текущий уровень масштабирования карты</summary>
    /// <remarks>Чем значение больше, тем уровень масштаба карты меньше: 10 уровень масштаба более мелкий, чем уровень 1</remarks>
    public double CurrentZoom
    {
        get => (double)GetValue(CurrentZoomProperty);
        set => SetValue(CurrentZoomProperty, value);
    }

    #endregion

    #region Property MaxScale : double - Максимально допустимый уровень визуального масштабирования

    public static readonly DependencyProperty MaxScaleProperty =
        DependencyProperty.Register(
            nameof(MaxScale),
            typeof(double),
            typeof(Map),
            new(double.NaN, null, OnCoerceMaxScale), OnValidateMaxScale);

    private static object OnCoerceMaxScale(DependencyObject d, object base_value)
    {
        if (base_value is not double scale || double.IsNaN(scale))
            return double.NaN;
        if (d.GetValue(MinScaleProperty) is double min_scale && !double.IsNaN(min_scale) && scale < min_scale)
            return min_scale;
        return scale;
    }

    private static bool OnValidateMaxScale(object value)
    {
        if (value is not double scale || double.IsNaN(scale))
            return true;
        return scale >= 0;
    }

    /// <summary>Максимально допустимый уровень визуального масштабирования</summary>
    public double MaxScale
    {
        get => (double)GetValue(MaxScaleProperty);
        set => SetValue(MaxScaleProperty, value);
    }

    #endregion

    #region Property MinScale : double - Максимально допустимый уровень визуального масштабирования

    public static readonly DependencyProperty MinScaleProperty =
        DependencyProperty.Register(
            nameof(MinScale),
            typeof(double),
            typeof(Map),
            new(double.NaN, null, OnCoerceMinScale), OnValidateMinScale);

    private static object OnCoerceMinScale(DependencyObject d, object base_value)
    {
        if (base_value is not double scale || double.IsNaN(scale))
            return double.NaN;
        if (d.GetValue(MaxScaleProperty) is double max_scale && !double.IsNaN(max_scale) && scale > max_scale)
            return max_scale;
        return scale;
    }

    private static bool OnValidateMinScale(object value)
    {
        if (value is not double scale || double.IsNaN(scale))
            return true;
        return scale >= 0;
    }

    /// <summary>Минимально допустимый уровень визуального масштабирования</summary>
    public double MinScale
    {
        get => (double)GetValue(MinScaleProperty);
        set => SetValue(MinScaleProperty, value);
    }

    #endregion

    /// <summary>Преобразует значение уровня масштаба в коэффициент визуального масштабирования</summary>
    /// <param name="value">Значение уровня масштаба для преобразования</param>
    /// <param name="targetType">Целевой тип значения</param>
    /// <param name="parameter">Параметр конвертера</param>
    /// <param name="culture">Культура для преобразования</param>
    /// <returns>Коэффициент масштабирования</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Equals(value, DependencyProperty.UnsetValue) || Equals(Binding.DoNothing))
            return value;

        if (value is not double zoom)
            throw new InvalidOperationException($"Значение должно быть типа {typeof(double)}, а не {value.GetType()}");

        var current_zoom = CurrentZoom;
        if (double.IsNaN(current_zoom)) // Если текущий уровень масштаба не задан, то масштабирование не выполняем
            return 1.0;

        var min_zoom = MinZoom;
        if (!double.IsNaN(min_zoom) && zoom < min_zoom)
            zoom = min_zoom;

        var max_zoom = MaxZoom;
        if (!double.IsNaN(max_zoom) && zoom > max_zoom)
            zoom = max_zoom;

        var delta = current_zoom - zoom;
        var scale = Math.Pow(2, delta);

        var min_scale = MinScale;
        if (!double.IsNaN(min_scale) && scale < min_scale)
            scale = min_scale;

        var max_scale = MaxScale;
        if (!double.IsNaN(max_scale) && scale > max_scale)
            scale = max_scale;

        return scale;
    }

    /// <summary>Преобразует коэффициент визуального масштабирования обратно в уровень масштаба</summary>
    /// <param name="value">Коэффициент масштабирования для обратного преобразования</param>
    /// <param name="type">Целевой тип значения</param>
    /// <param name="parameter">Параметр конвертера</param>
    /// <param name="culture">Культура для преобразования</param>
    /// <returns>Уровень масштаба</returns>
    public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
    {
        if (Equals(value, DependencyProperty.UnsetValue) || Equals(Binding.DoNothing))
            return value;

        if (value is not double scale)
            throw new InvalidOperationException($"Значение должно быть типа {typeof(double)}, а не {value.GetType()}");

        var current_zoom = CurrentZoom;
        if (double.IsNaN(current_zoom)) // Если текущий уровень масштаба не задан, то масштабирование не выполняем
            return double.NaN;

        var min_scale = MinScale;
        if (!double.IsNaN(min_scale) && scale < min_scale)
            scale = min_scale;

        var max_scale = MaxScale;
        if (!double.IsNaN(max_scale) && scale > max_scale)
            scale = max_scale;

#if NET8_0_OR_GREATER
        var delta = Math.Log2(scale);
#else
        var delta = Math.Log(scale, 2);
#endif

        var zoom = current_zoom - delta;

        var min_zoom = MinZoom;
        if (!double.IsNaN(min_zoom) && zoom < min_zoom)
            zoom = min_zoom;

        var max_zoom = MaxZoom;
        if (!double.IsNaN(max_zoom) && zoom > max_zoom)
            zoom = max_zoom;

        return zoom;
    }

    /// <summary>Создаёт новый экземпляр конвертера</summary>
    /// <returns>Новый экземпляр `Zoom`</returns>
    protected override Freezable CreateInstanceCore() => new Zoom
    {
        MaxZoom = MaxZoom,
        MinZoom = MinZoom,
        CurrentZoom = CurrentZoom,
        MinScale = MinScale,
        MaxScale = MaxScale,
    };
}
