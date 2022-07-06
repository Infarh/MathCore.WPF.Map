namespace MathCore.WPF.Map;

/// <summary>Элемент карты</summary>
public interface IMapElement
{
    /// <summary>Карта, которой принадлежит элемент</summary>
    MapBase ParentMap { get; set; }
}
