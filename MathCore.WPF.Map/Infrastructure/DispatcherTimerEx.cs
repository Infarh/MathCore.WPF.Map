using System.Windows.Threading;

namespace MathCore.WPF.Map.Infrastructure;

/// <summary>DispatcherTimer</summary>
internal static class DispatcherTimerEx
{
    public static DispatcherTimer WithTick(this DispatcherTimer timer, TimeSpan Interval, Action OnTick)
    {
        timer.Interval = Interval;
        timer.Tick += (_,_) => OnTick();
        return timer;
    }
}