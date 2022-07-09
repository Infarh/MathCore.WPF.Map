using System.Diagnostics;

using MathCore.WPF.Commands;
using MathCore.WPF.Dialogs;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.TestWPF.Commands;

public class FillCacheCommand : Command
{
    private Task? _ProcessingTask;

    public override bool CanExecute(object? parameter) => base.CanExecute(parameter) && _ProcessingTask is null or { IsCompleted: true };

    public override async void Execute(object? parameter)
    {
        var (map_layer, map_level) = parameter switch
        {
            Map { MapLayer: MapTileLayer layer, ZoomLevel: var level } => (layer, level),
            MapTileLayer layer => (layer, double.NaN),
            _ => (null, double.NaN)
        };

        if(map_layer is null) return;

        var task = LoadingCacheAsync(map_layer, (int)map_level);
        _ProcessingTask = task;
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {

        }
        catch(Exception error)
        {
            Debug.WriteLine(error);
        }
    }

    private static async Task LoadingCacheAsync(MapTileLayer Layer, int Level)
    {
        using var dialog = ProgressDialog.Show("Загрузка кеша тайлов", $"Загрузка тайлов уровня {Level}");

        await Task.Run(() => Layer.TileImageLoader.LoadTilesAsync(
                TileLayer: Layer, 
                    Level: Level, 
               CheckCache: true, 
               Expiration: DateTime.Now.AddYears(20), 
            ParallelLevel: 32,
                Progress: dialog.Progress, 
                  Status: dialog.Information,
                  Cancel: dialog.Cancel));
    }
}
