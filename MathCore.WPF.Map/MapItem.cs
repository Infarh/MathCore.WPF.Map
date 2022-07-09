using System.Windows.Controls;

namespace MathCore.WPF.Map;

/// <summary>Визуальный элемент карты</summary>
public class MapItem : ListBoxItem
{
    public MapItem()
    {
        DefaultStyleKey = typeof(MapItem);

        MapPanel.InitMapElement(this);
    }
}