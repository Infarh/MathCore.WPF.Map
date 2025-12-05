namespace MathCore.WPF.Map.TestWPF.Infrastructure;

internal static class DoubleEx
{
    public static double LimitMax(this ref double x, double max)
    {
        x = Math.Min(x, max);
        return x;
    }

    public static double LimitMin(this ref double x, double min)
    {
        x = Math.Max(min, x);
        return x;
    }
}
