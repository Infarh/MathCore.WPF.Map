using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map;

/// <summary>Загрузчик изображений тайлов</summary>
public interface ITileImageLoader
{
    /// <summary>Загружает изображения тайлов для текущей сетки слоя</summary>
    /// <param name="TileLayer">Слой тайлов</param>
    Task LoadTilesAsync(MapTileLayer TileLayer);

    /// <summary>Загружает изображения тайлов для указанного уровня</summary>
    /// <param name="TileLayer">Слой тайлов</param>
    /// <param name="Level">Уровень масштаба</param>
    /// <param name="CheckCache">Проверять кеш перед загрузкой</param>
    /// <param name="Expiration">Срок устаревания кэшированных данных</param>
    /// <param name="ParallelLevel">Максимальное число параллельных загрузок</param>
    /// <param name="Progress">Прогресс выполнения 0..1</param>
    /// <param name="Status">Строковый статус процесса</param>
    /// <param name="Cancel">Токен отмены</param>
    Task LoadTilesAsync(MapTileLayer TileLayer,
        int Level,
        bool CheckCache,
        DateTime? Expiration = null,
        int ParallelLevel = 32,
        IProgress<double>? Progress = null,
        IProgress<string>? Status = null,
        CancellationToken Cancel = default);

    /// <summary>Загружает изображения тайлов для диапазона уровней и координат</summary>
    /// <param name="TileLayer">Слой тайлов</param>
    /// <param name="LevelMin">Минимальный уровень масштаба</param>
    /// <param name="LevelMax">Максимальный уровень масштаба</param>
    /// <param name="XMin">Минимальный индекс X</param>
    /// <param name="YMin">Минимальный индекс Y</param>
    /// <param name="XMax">Максимальный индекс X</param>
    /// <param name="YMax">Максимальный индекс Y</param>
    /// <param name="CheckCache">Проверять кеш перед загрузкой</param>
    /// <param name="Expiration">Срок устаревания кэшированных данных</param>
    /// <param name="ParallelLevel">Максимальное число параллельных загрузок</param>
    /// <param name="Progress">Прогресс выполнения 0..1</param>
    /// <param name="Status">Строковый статус процесса</param>
    /// <param name="Cancel">Токен отмены</param>
    Task LoadTilesAsync(MapTileLayer TileLayer,
        int LevelMin, int LevelMax, 
        int XMin, int YMin, int XMax, int YMax,
        bool CheckCache,
        DateTime? Expiration = null,
        int ParallelLevel = 32,
        IProgress<double>? Progress = null,
        IProgress<string>? Status = null,
        CancellationToken Cancel = default);
}
