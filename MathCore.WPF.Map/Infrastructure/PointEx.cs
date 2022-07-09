using System.Windows;

namespace MathCore.WPF.Map.Infrastructure;

internal static class PointEx
{
    public static void Deconstruct(this Point point, out double X, out double Y)
    {
        X = point.X;
        Y = point.Y;
    }
}
