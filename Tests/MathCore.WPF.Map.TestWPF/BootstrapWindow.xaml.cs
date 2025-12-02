using System.Windows;

namespace MathCore.WPF.Map.TestWPF;

/// <summary>Bootstrap Styles Demo Window</summary>
public partial class BootstrapWindow : Window
{
    public BootstrapWindow()
    {
        InitializeComponent();
        LoadDemoPages();
    }

    private void LoadDemoPages()
    {
        ColorsFrame.Navigate(new Views.Bootstrap.ColorsDemo());
        TypographyFrame.Navigate(new Views.Bootstrap.TypographyDemo());
        ButtonsFrame.Navigate(new Views.Bootstrap.ButtonsDemo());
        FormsFrame.Navigate(new Views.Bootstrap.FormsDemo());
        GridFrame.Navigate(new Views.Bootstrap.GridDemo());
        SpacingFrame.Navigate(new Views.Bootstrap.SpacingDemo());
        BordersFrame.Navigate(new Views.Bootstrap.BordersDemo());
        ComponentsFrame.Navigate(new Views.Bootstrap.ComponentsDemo());
    }
}
