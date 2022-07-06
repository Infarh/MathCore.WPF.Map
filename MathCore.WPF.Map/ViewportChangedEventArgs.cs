using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map;

public class ViewportChangedEventArgs : EventArgs
{
    public ViewportChangedEventArgs(bool ProjectionChanged = false, double LongitudeOffset = 0d)
    {
        this.ProjectionChanged = ProjectionChanged;
        this.LongitudeOffset = LongitudeOffset;
    }

    /// <summary>Изменилась проекция карты</summary>
    /// <remarks>
    /// Если <see cref="MapTileLayer"/> или <see cref="MapImageLayer"/> должны быть
    /// немедленно обновлены, или MapPath Data в декартовой системе координат карты должны быть пересчитаны.
    /// </remarks>
    public bool ProjectionChanged { get; }

    /// <summary>Смещение долготы центра карты относительно предыдущего представления</summary>
    /// <remarks>Используется для определения того, что центр карты переместился на 180° по долготе</remarks>
    public double LongitudeOffset { get; }
}