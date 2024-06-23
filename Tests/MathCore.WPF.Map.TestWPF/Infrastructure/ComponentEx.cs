using System.Windows;
using System.Windows.Input;

namespace MathCore.WPF.Map.TestWPF.Infrastructure;

/// <summary>ComponentEx</summary>
public static class ComponentEx
{
    #region Attached property DoubleClickCommand : ICommand - Команда двойного щелчка мышки

    /// <summary>Команда двойного щелчка мышки</summary>
    [AttachedPropertyBrowsableForType(typeof(UIElement))]
    public static void SetDoubleClickCommand(DependencyObject d, ICommand value) => d.SetValue(DoubleClickCommandProperty, value);

    /// <summary>Команда двойного щелчка мышки</summary>
    public static ICommand GetDoubleClickCommand(DependencyObject d) => (ICommand)d.GetValue(DoubleClickCommandProperty);

    /// <summary>Команда двойного щелчка мышки</summary>
    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "DoubleClickCommand",
            typeof(ICommand),
            typeof(ComponentEx),
            new(default(ICommand), OnCommandChanged));

    private static void OnCommandChanged(DependencyObject D, DependencyPropertyChangedEventArgs E)
    {
        if (D is not UIElement element) return;
        element.MouseUp -= OnMouseUp;
        if (E.NewValue is { })
            element.MouseUp += OnMouseUp;
    }

    private static readonly DependencyProperty __LastClickTimeProperty =
        DependencyProperty.RegisterAttached(
            "__LastClickTime",
            typeof(DateTime?),
            typeof(ComponentEx),
            new(default(DateTime?)));

    private static void OnMouseUp(object Sender, MouseButtonEventArgs E)
    {
        if (Sender is not UIElement element || E.LeftButton != MouseButtonState.Released) return;

        var now = DateTime.Now;
        if (element.GetValue(__LastClickTimeProperty) is not DateTime last_click_time)
        {
            element.SetValue(__LastClickTimeProperty, now);
            return;
        }

        element.SetValue(__LastClickTimeProperty, now);

        if ((now - last_click_time).TotalSeconds > 1)
            return;

        if(GetDoubleClickCommand(element) is not { } command) return;

        var parameter = GetDoubleClickCommandPassEventArg(element) ? E : GetDoubleClickCommandParameter(element);
        if (!command.CanExecute(parameter)) return;
        command.Execute(parameter);
    }

    #endregion

    #region Attached property DoubleClickCommandParameter : object - Параметр команды двойного нажатия мышки

    /// <summary>Параметр команды двойного нажатия мышки</summary>
    [AttachedPropertyBrowsableForType(typeof(UIElement))]
    public static void SetDoubleClickCommandParameter(DependencyObject d, object value) => d.SetValue(DoubleClickCommandParameterProperty, value);

    /// <summary>Параметр команды двойного нажатия мышки</summary>
    public static object GetDoubleClickCommandParameter(DependencyObject d) => d.GetValue(DoubleClickCommandParameterProperty);

    /// <summary>Параметр команды двойного нажатия мышки</summary>
    public static readonly DependencyProperty DoubleClickCommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "DoubleClickCommandParameter",
            typeof(object),
            typeof(ComponentEx),
            new(default(object)));

    #endregion

    #region Attached property DoubleClickCommandPassEventArg : bool - Передавать параметр события в аргумент команды

    /// <summary>Передавать параметр события в аргумент команды</summary>
    [AttachedPropertyBrowsableForType(typeof(UIElement))]
    public static void SetDoubleClickCommandPassEventArg(DependencyObject d, bool value) => d.SetValue(DoubleClickCommandPassEventArgProperty, value);

    /// <summary>Передавать параметр события в аргумент команды</summary>
    public static bool GetDoubleClickCommandPassEventArg(DependencyObject d) => (bool)d.GetValue(DoubleClickCommandPassEventArgProperty);

    /// <summary>Передавать параметр события в аргумент команды</summary>
    public static readonly DependencyProperty DoubleClickCommandPassEventArgProperty =
        DependencyProperty.RegisterAttached(
            "DoubleClickCommandPassEventArg",
            typeof(bool),
            typeof(ComponentEx),
            new(default(bool)));

    #endregion
}