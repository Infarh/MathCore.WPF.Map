using System.Windows.Media;

namespace MathCore.WPF.Map.TileLayers;

/// <summary>Тайловый слой, использующий функциональный источник тайлов</summary>
public sealed class FunctionalTileLayer : MapTileLayer
{
    /// <summary>Конструктор по умолчанию для XAML</summary>
    public FunctionalTileLayer() { }

    /// <summary>Создаёт функциональный тайловый слой</summary>
    /// <param name="SourceName">Имя источника для кеша</param>
    /// <param name="Description">Описание слоя</param>
    /// <param name="TileFunc">Функция генерации тайлов</param>
    public FunctionalTileLayer(string SourceName, string Description,
        FunctionalTileSourceDelegate TileFunc)
    {
        this.SourceName = SourceName;
        this.Description = Description;
        this.TileSource = new FunctionalTileSource(TileFunc);
        this.MaxZoomLevel = 21;
    }
}
