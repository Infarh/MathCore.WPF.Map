using MathCore.Hosting;
using MathCore.WPF.ViewModels;

namespace MathCore.WPF.Map.TestWPF.ViewModels
{
    [Service]
    public class MainWindowViewModel : TitledViewModel
    {
        public MainWindowViewModel() => Title = "Главное окно";
    }
}
