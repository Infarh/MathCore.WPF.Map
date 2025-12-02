using System.Windows;
using System.Windows.Controls;

namespace MathCore.WPF.Map;

/// <summary>Набор элементов карты, каждому из которых можно передать управление (выбрать)</summary>
public class MapItemsControl : ListBox
{
    /// <summary>Создаёт контрол набора элементов карты и инициализирует связь с картой</summary>
    public MapItemsControl()
    {
        DefaultStyleKey = typeof(MapItemsControl);

        MapPanel.InitMapElement(this);
    }

    /// <summary>Создаёт контейнер элемента для элемента данных</summary>
    /// <returns>Контейнер элемента данных</returns>
    protected override DependencyObject GetContainerForItemOverride() => new MapItem();

    /// <summary>Проверяет, является ли элемент собственным контейнером</summary>
    /// <param name="item">Элемент данных</param>
    /// <returns>true, если элемент является собственным контейнером</returns>
    protected override bool IsItemItsOwnContainerOverride(object item) => item is MapItem;
}