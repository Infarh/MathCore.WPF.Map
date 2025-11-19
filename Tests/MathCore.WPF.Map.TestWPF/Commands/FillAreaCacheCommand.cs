using System.Diagnostics;

using MathCore.WPF.Commands;
using MathCore.WPF.Dialogs;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.TestWPF.Commands;

public class FillAreaCacheCommand : Command
{
    private Task? _ProcessingTask;

    public override bool CanExecute(object? parameter) => base.CanExecute(parameter) && _ProcessingTask is null or { IsCompleted: true };

    public override async void Execute(object? parameter)
    {
        if(parameter is not Map
           {
               MapLayer: MapTileLayer
               {
                   TileGrid:
                   {
                       XMin: var x_min, 
                       YMin: var y_min, 
                       XMax: var x_max,
                       YMax: var y_max
                   }
               } layer,
               ZoomLevel: var zoom_level,
               MaxZoomLevel: var max_zoom_level,
           })
            return;

        //var (map_layer, map_level) = parameter switch
        //{
        //    Map
        //    {
        //        MapLayer: MapTileLayer
        //        {
        //            TileGrid:
        //            {
        //                XMin: var x_min, 
        //                XMax: var x_max,
        //                YMin: var y_min,
        //                YMax: var y_max
        //            }
        //        } layer, 
        //        ZoomLevel: var level
        //    } => (layer, level),
        //    MapTileLayer layer => (layer, double.NaN),
        //    _ => (null, double.NaN)
        //};

        var task = LoadingCacheAsync(layer, (int)zoom_level, (int)max_zoom_level, x_min, y_min, x_max, y_max);
        _ProcessingTask = task;
        try
        {
            await task.ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
        }
    }

    private static async Task LoadingCacheAsync(MapTileLayer Layer, int LevelMin, int LevelMax, int XMin, int YMin, int XMax, int YMax)
    {
        using var dialog = ProgressDialog.Show("Загрузка кеша тайлов", $"Загрузка тайлов уровней {LevelMin} - {LevelMax}");

        await Task.Run(() => Layer.TileImageLoader.LoadTilesAsync(
                TileLayer: Layer,
                 LevelMin: LevelMin,
                 LevelMax: LevelMax,
                     XMin: XMin,
                     YMin: YMin,
                     XMax: XMax,
                     YMax: YMax,
               CheckCache: true,
               Expiration: DateTime.Now.AddYears(20),
            ParallelLevel: 32,
                 Progress: dialog.Progress,
                   Status: dialog.Information,
                   Cancel: dialog.Cancel))
           .ConfigureAwait(false);
    }
}
