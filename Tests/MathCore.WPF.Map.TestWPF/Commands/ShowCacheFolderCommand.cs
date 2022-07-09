using System.IO;

using MathCore.WPF.Commands;
using MathCore.WPF.Map.Caching;

namespace MathCore.WPF.Map.TestWPF.Commands;

public class ShowCacheFolderCommand : Command
{
    public override bool CanExecute(object? parameter) => TileImageLoader.Cache is ImageFileCache { RootFolder.Length: > 0 };

    public override void Execute(object? parameter)
    {
        if (TileImageLoader.Cache is not ImageFileCache { RootFolder: { Length: > 0 } dir_path } || new DirectoryInfo(dir_path) is not { Exists: true } root_dir) 
            return;

        var dir = root_dir;
        if (parameter is string { Length: > 0 } source_name && root_dir.SubDirectory(source_name) is { Exists: true } source_dir)
            dir = source_dir;

        dir.OpenInFileExplorer();
    }
}
