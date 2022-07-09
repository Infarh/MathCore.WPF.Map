using System.Windows;
using System.Windows.Controls;

namespace MathCore.WPF.Map;

/// <summary>Набор элементов карты, каждому из которых можно передать управление (выбрать)</summary>
public class MapItemsControl : ListBox
{
    public MapItemsControl()
    {
        DefaultStyleKey = typeof(MapItemsControl);

        MapPanel.InitMapElement(this);
    }

    protected override DependencyObject GetContainerForItemOverride() => new MapItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MapItem;
}