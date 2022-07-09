using MathCore.Hosting.WPF;
using MathCore.WPF.Map.TestWPF.ViewModels;

namespace MathCore.WPF.Map.TestWPF;

public class ServiceLocator : ServiceLocatorHosted
{
    public MainWindowViewModel MainModel => GetRequiredService<MainWindowViewModel>();
}
