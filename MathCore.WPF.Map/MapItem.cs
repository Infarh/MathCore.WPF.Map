using System.Windows.Controls;

namespace MathCore.WPF.Map;

/// <summary>Визуальный элемент карты</summary>
public class MapItem : ListBoxItem
{
    /// <summary>Создаёт визуальный элемент карты и инициализирует связь с картой</summary>
    public MapItem()
    {
        DefaultStyleKey = typeof(MapItem);

        MapPanel.InitMapElement(this);
    }
}