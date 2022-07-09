using System.Windows;

namespace MathCore.WPF.Map.Infrastructure;

internal static class SizeEx
{
    public static void Deconstruct(this Size size, out double Width, out double Height) => (Width, Height) = (size.Width, size.Height);
}