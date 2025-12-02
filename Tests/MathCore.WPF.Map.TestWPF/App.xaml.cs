using System.Windows;

using MathCore.WPF.Map.Caching;

namespace MathCore.WPF.Map.TestWPF;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        TileImageLoader.Cache = new ImageFileCache("maps");

        var start_window = e.Args.Length > 0 && e.Args[0] == "bootstrap" 
            ? new BootstrapWindow() 
            : new DarkStyleWindow() as Window;
        
        start_window.Show();
    }
}