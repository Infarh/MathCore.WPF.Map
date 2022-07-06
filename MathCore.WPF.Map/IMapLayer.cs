using System.Windows.Media;

namespace MathCore.WPF.Map;

/// <summary>Слой карты</summary>
public interface IMapLayer : IMapElement
{
    /// <summary>Задний фон слоя</summary>
    Brush MapBackground { get; }

    /// <summary>Основная кисть слоя для рисования визуальных элементов</summary>
    Brush MapForeground { get; }
}
