using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Windows.Media.Imaging;

using MathCore.WPF.Map.Caching;
using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.TileLayers;

using File = System.IO.File;

namespace MathCore.WPF.Map;

/// <summary>Загрузка и кеширвоание изображений тайлов карты для <see cref="MapTileLayer"/></summary>
public class TileImageLoader : ITileImageLoader
{
    /// <summary>По умолчанию путь к директории кеша тайлов C:\ProgramData\Map\TileCache</summary>
    public static string DefaultCacheFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MapControl", "TileCache");

    /// <summary>Кеш <see cref="ObjectCache"/> тайлов</summary>
    /// <remarks>По умолчанию <see cref="MemoryCache"/>.<see cref="MemoryCache.Default"/></remarks>
    public static ObjectCache Cache { get; set; } = MemoryCache.Default;

    /// <summary>Количество соединений по умолчанию</summary>
    private static int DefaultConnectionLimit => ServicePointManager.DefaultConnectionLimit;

    /// <summary>Время истечения времени действия кеша изображений</summary>
    /// <remarks>Используется когда время истечения действия не передаётся при скачивании. По умолчанию один день</remarks>
    public static TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Минимальное время действия кеша. По умолчанию один час.</summary>
    public static TimeSpan MinimumCacheExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Максимальное время действие кеша. По умолчанию один месяц.</summary>
    public static TimeSpan MaximumCacheExpiration { get; set; } = TimeSpan.FromDays(30);

    public static bool CheckCacheExpiration { get; set; } = true;

    /// <summary>Формат строки для создания ключа кеша для источника данных <see cref="TileSource"/></summary>
    /// <remarks>По умолчанию {0};zoom-{1};tile-{0}[z{1}x{2}y{3}]{4}</remarks>
    public static string CacheKeyFormat { get; set; } = "{0};zoom-{1};tile-{0}[z{1}x{2}y{3}]{4}";

    private readonly ConcurrentQueue<Tile> _PendingTiles = new();

    private int _TaskCount;

    private static async Task LoadTileImageAsync(Tile tile, Uri uri, string CacheKey, bool CheckCacheExpiration)
    {
        var buffer = GetCachedImage(CacheKey, out var expiration);
        var loaded = false;

        if (buffer is null || (CheckCacheExpiration && DateTime.UtcNow >= expiration))
        {
            var timer = Stopwatch.StartNew();
            loaded = await DownloadTileImageAsync(tile, uri, CacheKey).ConfigureAwait(true);
            timer.Stop();
            if (loaded)
                Debug.WriteLine("Загрузка тайла[x:{0},y:{1},z:{2}] из {3} выполнена успешно за {4}мс",
                    tile.X, tile.Y, tile.ZoomLevel,
                    uri.Host, timer.ElapsedMilliseconds);
            else
                Debug.WriteLine("Загрузка тайла[x:{0},y:{1},z:{2}] из {3} не выполнена",
                    tile.X, tile.Y, tile.ZoomLevel,
                    uri.Host);
        }

        if (!loaded && buffer is not null) // оставляем кешированное изображение если загрузка не удалась
        {
            var timer = Stopwatch.StartNew();
            using var stream = new MemoryStream(buffer);
            await SetTileImageAsync(tile, stream).ConfigureAwait(true);
            timer.Stop();
            Debug.WriteLine("Загрузка тайла[x:{0},y:{1},z:{2}] выполнение из кеша за {3}мс",
                tile.X, tile.Y, tile.ZoomLevel, timer.ElapsedMilliseconds);
        }
    }

    private static async Task<bool> DownloadTileImageAsync(Tile tile, Uri uri, string CacheKey)
    {
        var success = false;

        try
        {
            using var response = await TileSource.HttpClient.GetAsync(uri).ConfigureAwait(true);
            success = response.IsSuccessStatusCode;

            if (!success)
                Debug.WriteLine("TileImageLoader: {0}: {1} {2}", uri, (int)response.StatusCode, response.ReasonPhrase);
            else if (TileSource.TileAvailable(response.Headers))
            {
                using var stream = new MemoryStream();
                await response.Content.CopyToAsync(stream).ConfigureAwait(true);
                stream.Seek(0, SeekOrigin.Begin);

                await SetTileImageAsync(tile, stream).ConfigureAwait(true);

                SetCachedImage(CacheKey, stream, GetExpiration(response));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("TileImageLoader: {0}: {1}", uri, ex.Message);
        }

        return success;
    }

    private static async Task SetTileImageAsync(Tile tile, Stream stream)
    {
        var image_source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

        await Task.Run(() => tile.Image.Dispatcher.Invoke(() => tile.SetImage(image_source)));
    }

    private const string __ExpiresStr = "EXPIRES:";

    private static byte[] GetCachedImage(string CacheKey, out DateTime expiration)
    {
        var buffer = Cache.Get(CacheKey) as byte[];

        if (buffer is { Length: >= 16 } && Encoding.ASCII.GetString(buffer, buffer.Length - 16, 8) == __ExpiresStr)
            expiration = new DateTime(BitConverter.ToInt64(buffer, buffer.Length - 8), DateTimeKind.Utc);
        else
            expiration = DateTime.MinValue;

        return buffer;
    }

    private static readonly byte[] __ExpirationBytes = Encoding.ASCII.GetBytes(__ExpiresStr);

    private static async Task SetExpirationAsync(Stream stream, DateTime expiration, CancellationToken Cancel)
    {
        stream.Seek(0, SeekOrigin.End);
        await stream.WriteAsync(__ExpirationBytes, 0, 8, Cancel).ConfigureAwait(false);
        await using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
        writer.Write(expiration.Ticks);
    }

    private static void SetCachedImage(string CacheKey, MemoryStream stream, DateTime expiration)
    {
        stream.Seek(0, SeekOrigin.End);
        stream.Write(__ExpirationBytes, 0, 8);
        stream.Write(BitConverter.GetBytes(expiration.Ticks), 0, 8);

        Cache.Set(CacheKey, stream.ToArray(), new CacheItemPolicy { AbsoluteExpiration = expiration });
    }

    public async Task LoadTilesAsync(MapTileLayer TileLayer)
    {
        _PendingTiles.Clear();

        var tile_source = TileLayer.TileSource;
        var source_name = TileLayer.SourceName;
        var parallel_level = TileLayer.MaxParallelDownloads;
        if (parallel_level <= 0) parallel_level = DefaultConnectionLimit;
        var tiles = TileLayer.Tiles.Where(t => t.Pending);

        if (tile_source is not null && tiles.Any())
        {
            if (Cache is null || source_name is not { Length: > 0 } ||
                tile_source.UriFormat is null || !tile_source.UriFormat.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                await tiles.Select(tile => LoadTileImageAsync(tile_source, tile)).WhenAll();
            else
            {
                foreach (var tile in tiles)
                    _PendingTiles.Enqueue(tile);

                while (_TaskCount < Math.Min(_PendingTiles.Count, parallel_level))
                {
                    Interlocked.Increment(ref _TaskCount);

                    _ = Task.Run(async () =>
                    {
                        await LoadPendingTilesAsync(tile_source, source_name);

                        Interlocked.Decrement(ref _TaskCount);
                    });
                }
            }
        }
    }

    public static void ClearCache(string TileSourceName = null, CancellationToken Cancel = default)
    {
        Debug.WriteLine("Очистка кеша {0}", (object)TileSourceName);

        if (TileSourceName == "*") TileSourceName = null;
        int length;
        if (TileSourceName?.EndsWith('*') == true)
        {
            TileSourceName = TileSourceName.TrimEnd('*');
            length = TileSourceName.Length;
        }
        else if (TileSourceName is not { Length: > 0 })
            length = -1;
        else
        {
            TileSourceName = $"{TileSourceName};";
            length = TileSourceName.Length;
        }

        if (length < 0)
            foreach (var (key, _) in Cache)
                Cache.Remove(key);
        else
            foreach (var (key, _) in Cache)
                if (key.Length > length && string.Compare(key, 0, TileSourceName, 0, length) == 0)
                    Cache.Remove(key);

        Debug.WriteLine("Очистка кеша {0} выполнена", (object)TileSourceName);
    }

    private static async Task LoadTileImageAsync(TileSource TileSource, Tile tile)
    {
        tile.Pending = false;

        try
        {
            var image_source = await TileSource.LoadImageAsync(tile.XIndex, tile.Y, tile.ZoomLevel).ConfigureAwait(true);

            if (image_source is not null)
                tile.SetImage(image_source);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("TileImageLoader: {0}/{1}/{2}: {3}", tile.ZoomLevel, tile.XIndex, tile.Y, ex.Message);
        }
    }

    private async Task LoadPendingTilesAsync(TileSource TileSource, string SourceName)
    {
        var check_cache_expiration = CheckCacheExpiration;
        while (_PendingTiles.TryDequeue(out var tile))
        {
            tile.Pending = false;

            try
            {
                if (TileSource.GetUri(tile.XIndex, tile.Y, tile.ZoomLevel) is { LocalPath: var path } uri)
                {
                    if (Path.GetExtension(path) is not { Length: > 0 } extension || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                        extension = ".jpg";

                    var source_name = SourceName.Contains(';')
                        ? SourceName.Replace(';', '_')
                        : SourceName;

                    var cache_key = MakeCacheKey(source_name, tile.ZoomLevel, tile.XIndex, tile.Y, extension);

                    await LoadTileImageAsync(tile, uri, cache_key, check_cache_expiration).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("TileImageLoader: {0}/{1}/{2}: {3}", tile.ZoomLevel, tile.XIndex, tile.Y, e.Message);
            }
        }
    }

    private static string MakeCacheKey(string SourceName, int ZoomLevel, int X, int Y, string Extension) =>
        string.Format(CacheKeyFormat, SourceName, ZoomLevel, X, Y, Extension);

    private const int __DefaultParallelLevel = 32;

    public async Task LoadTilesAsync(MapTileLayer TileLayer,
        int Level,
        bool CheckCache,
        DateTime? Expiration = null,
        int ParallelLevel = __DefaultParallelLevel,
        IProgress<double> Progress = null,
        IProgress<string> Status = null,
        CancellationToken Cancel = default)
    {
        var line_count = 1 << Level;
        double total_count = line_count * line_count;

        var (SourceName, tile_source) = TileLayer.CheckAccess()
            ? (TileLayer.SourceName, TileLayer.TileSource)
            : TileLayer.Dispatcher.Invoke(() => (TileLayer.SourceName, TileLayer.TileSource));

        var source_name = SourceName.Contains(';')
            ? SourceName.Replace(';', '_')
            : SourceName;

        Debug.WriteLine("Загрузка тайлов для слоя {0}, уровня {1}", SourceName, Level);

        var client = TileSource.HttpClient;
        var operation_timer = Stopwatch.StartNew();
        if (ParallelLevel <= 0) ParallelLevel = __DefaultParallelLevel;
        var tasks = new List<Task>(ParallelLevel);
        if (Cache is ImageFileCache { RootFolder: var cache_root_folder } cache)
            cache.StoreInMemory = false;
        else
            return;

        try
        {
            for (var x = 0; x < line_count; x++)
            {
                double elapsed_s;
                TimeSpan elapsed;
                for (var y = 0; y < line_count; y++)
                {
                    Cancel.ThrowIfCancellationRequested();
                    if (tile_source.GetUri(x, y, Level) is not { LocalPath: var path } uri)
                        continue;

                    if (Path.GetExtension(path) is not { Length: > 0 } extension || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                        extension = ".jpg";

                    var cache_key = MakeCacheKey(source_name, Level, x, y, extension);
                    if (CheckCache && Cache.Contains(cache_key))
                    {
                        Debug.WriteLine("Тайл[x:{0},y:{1},z:{2}] уже присутствует в кеше", x, y, Level);
                        Cancel.ThrowIfCancellationRequested();
                        continue;
                    }

                    tasks.Add(LoadTileAsync(client, uri, x, y, Level, line_count, cache_key, cache_root_folder, Expiration, Cancel));
                    static async Task LoadTileAsync(
                        HttpClient client,
                        Uri uri,
                        int x, int y, int Level,
                        int line_count,
                        string cache_key,
                        string cache_root_folder,
                        DateTime? Expiration,
                        CancellationToken Cancel)
                    {
                        var timer = Stopwatch.StartNew();
                        using var response = await client.GetAsync(uri, Cancel).ConfigureAwait(false);
                        timer.Stop();
                        if (!response.IsSuccessStatusCode)
                        {
                            Debug.WriteLine("Тайл[x:{0},y:{1},z:{2}] не удалось загрузить с сервера {3}", x, y, Level, uri.Host);
                            return;
                        }

                        using var stream = new MemoryStream();
                        await response.Content.CopyToAsync(stream, Cancel);
                        stream.Seek(0, SeekOrigin.Begin);

                        await SetExpirationAsync(stream, Expiration ?? GetExpiration(response), Cancel);

                        var file_path = ImageFileCache.GetFilePath(cache_root_folder, cache_key);

                        await SaveFileDataAsync(stream, file_path, Cancel);

                        Debug.WriteLine("Тайл[x:{0}/{3},y:{1}/{3},z:{2}] загружен из {4} за {5} мс",
                            x, y, Level, line_count, uri.Host, timer.ElapsedMilliseconds);
                    }

                    if (tasks.Count >= __DefaultParallelLevel)
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                        tasks.Clear();
                        Progress?.Report((x * line_count + y + 1) / total_count);

                        elapsed_s = (line_count - (x + 1)) / (x + 1d) * operation_timer.Elapsed.TotalSeconds;
                        elapsed = TimeSpan.FromSeconds(elapsed_s);
                        Status?.Report($"Загрузка x:{x}, y:{y} / {line_count}\r\nПрошло {operation_timer.Elapsed:h\\:mm\\:ss\\.f} осталось {elapsed:h\\:mm\\:ss\\.f}");
                        Progress?.Report((x + 1) * line_count / total_count);
                    }
                }

                elapsed_s = (line_count - (x + 1)) / (x + 1d) * operation_timer.Elapsed.TotalSeconds;
                elapsed = TimeSpan.FromSeconds(elapsed_s);
                Status?.Report($"Загрузка строки {x + 1}/{line_count}\r\nПрошло {operation_timer.Elapsed:h\\:mm\\:ss\\.f} осталось {elapsed:h\\:mm\\:ss\\.f}");
                Progress?.Report((x + 1) * line_count / total_count);
            }
        }
        finally
        {
            if (cache is not null)
                cache.StoreInMemory = true;
        }
    }

    public async Task LoadTilesAsync(MapTileLayer TileLayer,
        int LevelMin,
        int LevelMax,
        int XMin,
        int YMin,
        int XMax,
        int YMax,
        bool CheckCache,
        DateTime? Expiration = null,
        int ParallelLevel = 32,
        IProgress<double> Progress = null,
        IProgress<string> Status = null,
        CancellationToken Cancel = default)
    {
        var (SourceName, tile_source) = TileLayer.CheckAccess()
            ? (TileLayer.SourceName, TileLayer.TileSource)
            : TileLayer.Dispatcher.Invoke(() => (TileLayer.SourceName, TileLayer.TileSource));

        var source_name = SourceName.Contains(';')
            ? SourceName.Replace(';', '_')
            : SourceName;

        Debug.WriteLine("Загрузка тайлов для слоя {0}, уровня {1} | {2}", SourceName, LevelMin, Environment.CurrentManagedThreadId);

        var client = TileSource.HttpClient;
        var operation_timer = Stopwatch.StartNew();
        if (ParallelLevel <= 0) ParallelLevel = __DefaultParallelLevel;
        var tasks = new List<Task>(ParallelLevel);

        if (Cache is ImageFileCache { RootFolder: var cache_root_folder } cache)
            cache.StoreInMemory = false;
        else
            return;

        var total_tiles_count = 0d;
        for (var z = LevelMin; z <= LevelMax; z++)
        {
            var k = 1 << (z - LevelMin);
            var x_min = (int)Math.Floor((double)XMin * k);
            var x_max = XMax * k;
            var y_min = Math.Max(YMin * k, 0);
            var y_max = Math.Min(YMax * k, (1 << z) - 1);

            total_tiles_count += (x_max - x_min) * (y_max - y_min);
        }

        try
        {
            var count = 0;
            for (var z = LevelMin; z <= LevelMax; z++)
            {
                Cancel.ThrowIfCancellationRequested();
                var k = 1 << (z - LevelMin);
                var x_min = (int)Math.Floor((double)XMin * k);
                var x_max = XMax * k;
                var y_min = Math.Max(YMin * k, 0);
                var y_max = Math.Min(YMax * k, (1 << z) - 1);

                for (var x = x_min; x < x_max; x++)
                {
                    Cancel.ThrowIfCancellationRequested();
                    for (var y = y_min; y <= y_max; y++)
                    {
                        count++;
                        Cancel.ThrowIfCancellationRequested();
                        if (tile_source.GetUri(x, y, z) is not { LocalPath: var path } uri)
                            continue;

                        if (Path.GetExtension(path) is not { Length: > 0 } extension || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                            extension = ".jpg";

                        var cache_key = MakeCacheKey(source_name, z, x, y, extension);
                        TimeSpan elapsed;
                        double elapsed_s;
                        if (CheckCache && Cache.Contains(cache_key))
                        {
                            elapsed_s = operation_timer.Elapsed.TotalSeconds * (total_tiles_count - count) / count;
                            elapsed = TimeSpan.FromSeconds(elapsed_s);
                            Debug.WriteLine("Тайл[x:{0},y:{1},z:{2}] уже присутствует в кеше {3}/{4} {5:p2} - {6}",
                                x, y, z, 
                                count, total_tiles_count, 
                                count / total_tiles_count, 
                                Environment.CurrentManagedThreadId);
                            Cancel.ThrowIfCancellationRequested();
                            Status?.Report($"Уже загружен x:{x}, y:{y}, z:{z}\r\nПрошло {operation_timer.Elapsed:h\\:mm\\:ss\\.f} осталось {elapsed:h\\:mm\\:ss\\.f}");
                            //Progress?.Report(Math.Min(count / total_tiles_count, 1));
                            continue;
                        }

                        tasks.Add(LoadTileAsync(client, uri, x, y, z, cache_key, cache_root_folder, Expiration, Cancel));
                        static async Task LoadTileAsync(
                            HttpClient client,
                            Uri uri,
                            int x, int y, int Level,
                            string cache_key,
                            string cache_root_folder,
                            DateTime? Expiration,
                            CancellationToken Cancel)
                        {
                            var timer = Stopwatch.StartNew();
                            using var response = await client.GetAsync(uri, Cancel).ConfigureAwait(false);
                            timer.Stop();
                            if (!response.IsSuccessStatusCode)
                            {
                                Debug.WriteLine("Тайл[x:{0},y:{1},z:{2}] не удалось загрузить с сервера {3}", x, y, Level, uri.Host);
                                return;
                            }

                            using var stream = new MemoryStream();
                            await response.Content.CopyToAsync(stream, Cancel);
                            stream.Seek(0, SeekOrigin.Begin);

                            await SetExpirationAsync(stream, Expiration ?? GetExpiration(response), Cancel);

                            var file_path = ImageFileCache.GetFilePath(cache_root_folder, cache_key);

                            await SaveFileDataAsync(stream, file_path, Cancel);

                            Debug.WriteLine("Тайл[x:{0},y:{1},z:{2}] загружен из {3} за {4} мс - {5}",
                                x, y, Level,
                                uri.Host, timer.ElapsedMilliseconds,
                                Environment.CurrentManagedThreadId);
                        }

                        if (tasks.Count < __DefaultParallelLevel) continue;

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                        tasks.Clear();
                        Debug.WriteLine("Прогресс {0}/{1} - {2:p2} - {3}",
                            count, total_tiles_count, count / total_tiles_count,
                            Environment.CurrentManagedThreadId);
                        Progress?.Report(Math.Min(count / total_tiles_count, 1));

                        elapsed_s = operation_timer.Elapsed.TotalSeconds * (total_tiles_count - count) / count;
                        elapsed = TimeSpan.FromSeconds(elapsed_s);
                        Status?.Report($"Загрузка x:{x}, y:{y}, z:{z}\r\nПрошло {operation_timer.Elapsed:h\\:mm\\:ss\\.f} осталось {elapsed:h\\:mm\\:ss\\.f}");
                    }
                }
            }
        }
        finally
        {
            if (cache is not null)
                cache.StoreInMemory = true;
        }
    }

    private static async Task SaveFileDataAsync(Stream Data, string FileName, CancellationToken Cancel = default)
    {
        try
        {
            Data.Seek(0, SeekOrigin.Begin);
            Directory.CreateDirectory(Path.GetDirectoryName(FileName)!);
            await using var file = File.Create(FileName);
            await Data.CopyToAsync(file, Cancel).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            File.Delete(FileName);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
    }

    private static DateTime GetExpiration(HttpResponseMessage response)
    {
        if (response is not { Headers.CacheControl.MaxAge: { } max_age })
            return DateTime.UtcNow.Add(DefaultCacheExpiration);

        var minimum_cache_expiration = MinimumCacheExpiration;
        var maximum_cache_expiration = MaximumCacheExpiration;

        if (max_age < minimum_cache_expiration)
            return DateTime.UtcNow.Add(minimum_cache_expiration);

        if (max_age > maximum_cache_expiration)
            return DateTime.UtcNow.Add(maximum_cache_expiration);

        return DateTime.UtcNow.Add(max_age);
    }
}