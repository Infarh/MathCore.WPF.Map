using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.Caching;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MathCore.WPF.Map.Caching;

/// <summary>Файловый кеш данных</summary>
public class ImageFileCache : ObjectCache
{
    /// <summary>Правило проверки полного доступа к файлу</summary>
    private static readonly FileSystemAccessRule __FullControlRule = new(
        new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
        FileSystemRights.FullControl, AccessControlType.Allow);

    /// <summary>Кеш данных в памяти</summary>
    private readonly MemoryCache _MemoryCache = MemoryCache.Default;

    /// <summary>Путь к базовой директории с файлами кеша</summary>
    public string RootFolder { get; }

    /// <summary>Использовать кеш в памяти</summary>
    public bool StoreInMemory { get; set; } = true;

    /// <inheritdoc />
    public override string Name => Path.GetFileName(RootFolder);

    /// <inheritdoc />
    public override DefaultCacheCapabilities DefaultCacheCapabilities => DefaultCacheCapabilities.None;

    public override object this[string key]
    {
        get => Get(key);
        set => Set(key, value, DateTimeOffset.Now.AddDays(30));
    }

    /// <summary>Инициализация нового экземпляра файлового кеша</summary>
    /// <param name="RootFolder">Путь к базовой директории с файлами кеша</param>
    /// <exception cref="ArgumentException">Возникает в случае если не указан путь к директории</exception>
    public ImageFileCache(string RootFolder)
    {
        if (string.IsNullOrEmpty(RootFolder))
            throw new ArgumentException("The parameter rootFolder must not be null or empty.");

        this.RootFolder = RootFolder;

        Debug.WriteLine("Файловый кеш создан в директории {0}", (object)RootFolder);
    }

    /// <summary>Перечисление содержимого кеша (возвращает объекты с указанием ключа и пустым значением</summary>
    protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        var root = new DirectoryInfo(RootFolder);
        if (!root.Exists)
            yield break;

        var base_path_dir_length = root.FullName.Length + 1;
        foreach (var file in root.EnumerateFiles("*.*", SearchOption.AllDirectories))
        {
            var key = file.FullName[base_path_dir_length..].Replace('\\', ';').Replace('/', ';');
            //var buffer = File.ReadAllBytes(file.FullName);
            //yield return new(key, buffer);
            yield return new(key, null);
        }
    }

    /// <inheritdoc />
    public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string RegionName = null) =>
        throw new NotSupportedException("Мониторы не поддерживаются");

    /// <inheritdoc />
    public override long GetCount(string RegionName = null) => Directory.EnumerateFiles(Path.GetFullPath(RootFolder), "*.*", SearchOption.AllDirectories).Count();

    /// <inheritdoc />
    public override bool Contains(string key, string RegionName = null)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (RegionName is not null)
            throw new NotSupportedException("Регины не поддерживаются");

        return _MemoryCache.Contains(key) || FindFile(key) is not null;
    }

    /// <inheritdoc />
    public override object Get(string key, string RegionName = null)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (RegionName is not null)
            throw new NotSupportedException("Регины не поддерживаются");

        var buffer = _MemoryCache.Get(key) as byte[];

        if (buffer is not null) return buffer;
        var path = FindFile(key);

        if (path is null) return null;
        try
        {
            buffer = File.ReadAllBytes(path);
            if (StoreInMemory)
                _MemoryCache.Set(key, buffer, new CacheItemPolicy());
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ImageFileCache: Ошибка чтения файла {0}: {1}", path, ex.Message);
        }

        return buffer;
    }

    /// <inheritdoc />
    public override CacheItem GetCacheItem(string key, string RegionName = null) =>
        Get(key, RegionName) is { } value
            ? new CacheItem(key, value)
            : null;

    /// <inheritdoc />
    public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string RegionName = null) => keys
       .ToDictionary(key => key, key => Get(key, RegionName));

    /// <inheritdoc />
    public override void Set(string key, object value, CacheItemPolicy policy, string RegionName = null)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (RegionName is not null)
            throw new NotSupportedException("Регины не поддерживаются");

        if (value is not byte[] buffer || buffer.Length == 0)
            throw new NotSupportedException("Массив не может быть нулевой длины");

        if (StoreInMemory)
        {
            if ((policy.AbsoluteExpiration - DateTimeOffset.Now).TotalHours > 1)
                policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) };
            _MemoryCache.Set(key, buffer, policy);
        }

        var path = GetFilePath(key);

        if (path is null) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, buffer);

            var file = new FileInfo(path);
            var file_security = file.GetAccessControl();
            file_security.AddAccessRule(__FullControlRule);
            file.SetAccessControl(file_security);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ImageFileCache: Ошибка чтения файла {0}: {1}", path, ex.Message);
        }
    }

    /// <inheritdoc />
    public override void Set(string key, object value, DateTimeOffset AbsoluteExpiration, string RegionName = null) =>
        Set(key, value, new CacheItemPolicy { AbsoluteExpiration = AbsoluteExpiration }, RegionName);

    /// <inheritdoc />
    public override void Set(CacheItem item, CacheItemPolicy policy) => Set(item.Key, item.Value, policy, item.RegionName);

    /// <inheritdoc />
    public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string RegionName = null)
    {
        var old_value = Get(key, RegionName);

        Set(key, value, policy);

        return old_value;
    }

    /// <inheritdoc />
    public override object AddOrGetExisting(string key, object value, DateTimeOffset AbsoluteExpiration, string RegionName = null) =>
        AddOrGetExisting(key, value, new CacheItemPolicy { AbsoluteExpiration = AbsoluteExpiration }, RegionName);

    public override CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy)
    {
        var old_item = GetCacheItem(item.Key, item.RegionName);

        Set(item, policy);

        return old_item ?? throw new InvalidOperationException();
    }

    /// <inheritdoc />
    public override object Remove(string key, string RegionName = null)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (RegionName is not null)
            throw new NotSupportedException("Регины не поддерживаются");

        if (StoreInMemory)
            _MemoryCache.Remove(key);

        var path = FindFile(key);

        if (path is null) return null;

        try
        {
            File.Delete(path);
            RemoveEmptyDirectory(Path.GetDirectoryName(path));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ImageFileCache: Ошибка чтения файла {0}: {1}", path, ex.Message);
        }

        return null;
    }

    /// <summary>Удалить пустую директорию</summary>
    /// <param name="path">Путь к удаляемой директории</param>
    private void RemoveEmptyDirectory(string path)
    {
        while (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
        {
            if (Path.GetFullPath(path) == Path.GetFullPath(RootFolder))
            {
                Directory.Delete(path, true);
                break;
            }

            Directory.Delete(path, true);
            path = Path.GetDirectoryName(path);
        }
    }

    /// <summary>Определить путь к файлу по его ключу</summary>
    /// <param name="key">Ключ файла</param>
    /// <returns>Путь к файлу, соответствующему заданному ключу</returns>
    private string FindFile(string key)
    {
        var path = GetFilePath(key);

        try
        {
            if (File.Exists(path))
                return path;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ImageFileCache: Ошибка при попытке поиска файла {0}: {1}", path, ex.Message);
        }

        return null;
    }

    /// <summary>Разделители элементов ключа</summary>
    private static readonly char[] __Separators = { '\\', '/', ':', ';' };

    /// <summary>Пул путей к файлам</summary>
    private readonly ConcurrentDictionary<string, string> _Paths = new();

    /// <summary>Сформировать путь к файлу на основе ключа</summary>
    /// <param name="Key">Ключ файла</param>
    /// <returns>ПУть к файлу</returns>
    private string GetFilePath(string Key)
    {
        try
        {
#if NETCOREAPP3_0_OR_GREATER
            return _Paths.GetOrAdd(
                key: Key,
                valueFactory: static (key, v) => GetFilePath(key, v),
                factoryArgument: (Root: RootFolder, Searators: __Separators));
#else
             return _Paths.GetOrAdd(
                            key: Key,
                            valueFactory: key => GetFilePath(key, (Root: RootFolder, Searators: __Separators)));
#endif

        }
        catch (Exception ex)
        {
            Debug.WriteLine("ImageFileCache: Неверный формат ключа {0}/{1}: {2}", RootFolder, Key, ex.Message);
        }

        return null;
    }

    /// <summary>Формирование пути к файлу на основе ключа и массива разделителей</summary>
    /// <param name="key">Ключ</param>
    /// <param name="v">Кортеж, содержащий путь к базовой директории и массив разделителей элементов ключа</param>
    /// <returns>Путь к файлу</returns>
    private static string GetFilePath(string key, (string Root, char[] Searators) v) => Path.Combine(v.Root, Path.Combine(key.Split(v.Searators)));

    /// <summary>Получить путь к файлу относительно базовой директории</summary>
    /// <param name="RootFolder">Базовый каталог</param>
    /// <param name="Key">Ключ файла</param>
    /// <returns>Путь к файлу в базовом каталоге</returns>
    public static string GetFilePath(string RootFolder, string Key) => GetFilePath(Key, (RootFolder, __Separators));
}