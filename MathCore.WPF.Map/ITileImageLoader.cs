using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map;

public interface ITileImageLoader
{
    Task LoadTilesAsync(MapTileLayer TileLayer);

    Task LoadTilesAsync(MapTileLayer TileLayer,
        int Level,
        bool CheckCache,
        DateTime? Expiration = null,
        int ParallelLevel = 32,
        IProgress<double> Progress = null,
        IProgress<string> Status = null,
        CancellationToken Cancel = default);

    Task LoadTilesAsync(MapTileLayer TileLayer,
        int LevelMin, int LevelMax, 
        int XMin, int YMin, int XMax, int YMax,
        bool CheckCache,
        DateTime? Expiration = null,
        int ParallelLevel = 32,
        IProgress<double> Progress = null,
        IProgress<string> Status = null,
        CancellationToken Cancel = default);
}
