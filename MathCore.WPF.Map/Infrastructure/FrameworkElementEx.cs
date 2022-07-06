using System.Windows.Data;

// ReSharper disable once CheckNamespace
namespace System.Windows;

internal static class FrameworkElementEx
{
    public static void SetBinding(this FrameworkElement element, DependencyProperty Property, string Path, object Source) =>
        element.SetBinding(Property, new Binding(Path) { Source = Source });
}
