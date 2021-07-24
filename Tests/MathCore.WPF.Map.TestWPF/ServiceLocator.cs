using System.Reflection;

using MathCore.Hosting.WPF;
using MathCore.WPF.Map.TestWPF.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace MathCore.WPF.Map.TestWPF
{
    public class ServiceLocator : ServiceLocatorHosted
    {
        public MainWindowViewModel MainModel => Services.GetRequiredService<MainWindowViewModel>();
    }
}
