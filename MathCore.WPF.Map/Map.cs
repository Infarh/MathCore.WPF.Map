#nullable enable
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map;

/// <summary>Карта</summary>
public class Map : MapBase
{
    static Map() => IsManipulationEnabledProperty.OverrideMetadata(typeof(Map), new FrameworkPropertyMetadata(true));

    #region Property ManipulationMode : ManipulationModes - Способы взаимодействия пользователя с картой

    public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty
       .Register(
            nameof(ManipulationMode),
            typeof(ManipulationModes),
            typeof(Map),
            new(ManipulationModes.All));

    /// <summary>Способы взаимодействия пользователя с картой</summary>
    public ManipulationModes ManipulationMode
    {
        get => (ManipulationModes)GetValue(ManipulationModeProperty);
        set => SetValue(ManipulationModeProperty, value);
    }

    #endregion

    #region Property MouseWheelZoomDelta : double - Коэффициент приближения/удаления колёсиком мышки

    public static readonly DependencyProperty MouseWheelZoomDeltaProperty = DependencyProperty
       .Register(
            nameof(MouseWheelZoomDelta),
            typeof(double),
            typeof(Map),
            new(1d));

    /// <summary>Коэффициент приближения/удаления колёсиком мышки</summary>
    public double MouseWheelZoomDelta
    {
        get => (double)GetValue(MouseWheelZoomDeltaProperty);
        set => SetValue(MouseWheelZoomDeltaProperty, value);
    }

    #endregion

    #region Property CursorPosition : Location - Положение курсора на карте

    /// <summary>Key of dependency property: Положение курсора на карте</summary>
    private static readonly DependencyPropertyKey __CursorPositionPropertyKey = DependencyProperty
       .RegisterReadOnly(
            nameof(CursorPosition),
            typeof(Location?),
            typeof(Map),
            new FrameworkPropertyMetadata(null));

    /// <summary>Положение курсора на карте</summary>
    public static readonly DependencyProperty CursorPositionProperty = __CursorPositionPropertyKey.DependencyProperty;

    /// <summary>Положение курсора на карте</summary>
    public Location? CursorPosition
    {
        get => (Location?)GetValue(CursorPositionProperty);
        private set => SetValue(__CursorPositionPropertyKey, value);
    } 

    #endregion

    #region Property CanMouseMove : bool - Взаимодействие мышки с картой активно

    /// <summary>Взаимодействие мышки с картой активно</summary>
    //[Category("")]
    [Description("Взаимодействие мышки с картой активно")]
    public bool CanMouseMove { get => (bool)GetValue(CanMouseMoveProperty); set => SetValue(CanMouseMoveProperty, value); }

    /// <summary>Взаимодействие мышки с картой активно</summary>
    public static readonly DependencyProperty CanMouseMoveProperty =
        DependencyProperty.Register(
            nameof(CanMouseMove),
            typeof(bool),
            typeof(Map),
            new(true));

    #endregion

    #region Property DoubleLeftMouseCommand : ICommand - Команда, выполняемая при двойном нажатии левой клавиши мышки

    /// <summary>Команда, выполняемая при двойном нажатии левой клавиши мышки</summary>
    //[Category("")]
    [Description("Команда, выполняемая при двойном нажатии левой клавиши мышки")]
    public ICommand? DoubleLeftMouseCommand { get => (ICommand?)GetValue(DoubleLeftMouseCommandProperty); set => SetValue(DoubleLeftMouseCommandProperty, value); }

    /// <summary>Команда, выполняемая при двойном нажатии левой клавиши мышки</summary>
    public static readonly DependencyProperty DoubleLeftMouseCommandProperty =
        DependencyProperty.Register(
            nameof(DoubleLeftMouseCommand),
            typeof(ICommand),
            typeof(Map),
            new(default(ICommand)));

    #endregion

    private Point? _MouseDownPosition;

    /// <summary>Обработка изменения положения колёсика мышки</summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if(!CanMouseMove) return;

        var zoom_delta = MouseWheelZoomDelta * e.Delta / 120;
        ZoomMap(e.GetPosition(this), TargetZoomLevel + zoom_delta);
    }

    /// <summary>Обработка события нажатия левой клавиши мышки</summary>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var position = e.GetPosition(this);
        var location = ViewportPointToLocation(position);

        if (e.ClickCount == 2 && 
            DoubleLeftMouseCommand is { } left_double_click_command && 
            left_double_click_command.CanExecute(location))
        {
            left_double_click_command.Execute(location);
            return;
        }

        if (!CaptureMouse()) return;

        _MouseDownPosition = position;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_MouseDownPosition is null) return;

        _MouseDownPosition = null;
        ReleaseMouseCapture();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        CursorPosition = ViewportPointToLocation(e.GetPosition(this));

        if(!CanMouseMove) return;
        if (_MouseDownPosition is not { } mouse_position) return;

        var position = e.GetPosition(this);
        TranslateMap(position - mouse_position);
        _MouseDownPosition = position;
    }

    protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
    {
        base.OnManipulationStarted(e);

        Manipulation.SetManipulationMode(this, ManipulationMode);
    }

    protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
    {
        base.OnManipulationDelta(e);

        TransformMap(
                 center: e.ManipulationOrigin,
            translation: e.DeltaManipulation.Translation, 
               rotation: e.DeltaManipulation.Rotation,
                  scale: 0.5 * (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y));
    }
}