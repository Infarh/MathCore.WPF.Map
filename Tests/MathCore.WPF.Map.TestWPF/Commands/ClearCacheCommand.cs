using System.Diagnostics;

using MathCore.WPF.Commands;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.TestWPF.Commands;

public class ClearCacheCommand : Command
{
    private Task? _ClearingTask;

    public override bool CanExecute(object? parameter) => _ClearingTask is null or { IsCompleted: true };

    public override async void Execute(object? parameter)
    {
        var tile_source_name = parameter switch
        {
            Map { MapLayer: MapTileLayer { SourceName: { Length: > 0 } source_name } } => source_name,
            MapTileLayer { SourceName: { Length: > 0 } source_name } => source_name,
            string { Length: > 0 } str => str,
            _ => null
        };

        var task = Task.Run(() => TileImageLoader.ClearCache(tile_source_name));
        _ClearingTask = task;
        OnCanExecuteChanged();
        try
        {
            await task.ConfigureAwait(true);
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
        }

        _ClearingTask = null;
        OnCanExecuteChanged();
    }
}
