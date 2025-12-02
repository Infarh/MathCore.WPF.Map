using System.Windows;

namespace MathCore.WPF.Map.TestWPF;

/// <summary>Демонстрационное окно для стилей тёмного меню</summary>
public partial class DarkStyleWindow : Window
{
    public DarkStyleWindow()
    {
        InitializeComponent();
    }

    private void OpenBootstrapWindow_Click(object Sender, RoutedEventArgs E)
    {
        var bootstrap_window = new BootstrapWindow();
        bootstrap_window.Show();
    }

    private void CloseWindow_Click(object Sender, RoutedEventArgs E) => Close();
}
