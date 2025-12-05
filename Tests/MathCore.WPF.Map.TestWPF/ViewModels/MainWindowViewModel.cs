using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Windows.Input;

using MathCore.DI;
using MathCore.Geolocation;
using MathCore.WPF.Commands;
using MathCore.WPF.Map.Extensions;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.TestWPF.Commands;
using MathCore.WPF.Map.TileLayers;
using MathCore.WPF.ViewModels;

using Microsoft.Win32;

namespace MathCore.WPF.Map.TestWPF.ViewModels;

[Service]
public class MainWindowViewModel() : TitledViewModel("Главное окно")
{
    /// <summary>Центр карты</summary>
    public Location? MapCenter { get; set => Set(ref field, value); } = new(55.65505, 37.7578);

    /// <summary>Курсор на карте</summary>
    public Location? MapCursorPosition { get; set => Set(ref field, value); }

    #region Command ClearCacheCommand - Очистка кеша

    /// <summary>Очистка кеша</summary>
    private ICommand? _ClearCacheCommand;

    /// <summary>Очистка кеша</summary>
    public ICommand ClearCacheCommand => _ClearCacheCommand ??= new ClearCacheCommand();

    #endregion

    public ObservableCollection<Location> Locations { get; } = [];

    #region SelectedLocation : Location? - Выбранное из списка положение

    /// <summary>Выбранное из списка положение</summary>
    private Location? _SelectedLocation;

    /// <summary>Выбранное из списка положение</summary>
    public Location? SelectedLocation { get => _SelectedLocation; set => Set(ref _SelectedLocation, value); }

    #endregion

    #region Command AddLocationCommand : Location - Добавить точку на карте

    /// <summary>Добавить точку на карте</summary>
    private Command? _AddLocationCommand;

    /// <summary>Добавить точку на карте</summary>
    public ICommand AddLocationCommand => _AddLocationCommand ??= Command.New<Location>(Location =>
    {
        Locations.Add(Location!);
        SelectedLocation = Location;
        UpdateLocationsListLength();
    });

    #endregion

    #region Command RemoveLocationCommand : Location - Удаление положения из списка

    /// <summary>Удаление положения из списка</summary>
    private Command? _RemoveLocationCommand;

    /// <summary>Удаление положения из списка</summary>
    public ICommand RemoveLocationCommand => _RemoveLocationCommand ??= Command.New<Location>(p =>
    {
        Locations.Remove(p!);
        UpdateLocationsListLength();
    });

    #endregion

    #region Command ClearLocationsCommand - Очистить положения

    /// <summary>Очистить положения</summary>
    private LambdaCommand? _ClearLocationsCommand;

    /// <summary>Очистить положения</summary>
    public ICommand ClearLocationsCommand => _ClearLocationsCommand ??= new(() =>
    {
        Locations.Clear();
        UpdateLocationsListLength();
    });

    #endregion

    #region LocationsListPathLength : double - Длина пути

    /// <summary>Длина пути</summary>
    private double _LocationsListPathLength;

    /// <summary>Длина пути</summary>
    public double LocationsListPathLength
    {
        get => _LocationsListPathLength;
        private set => Set(ref _LocationsListPathLength, value);
    }

    #endregion

    private Location _FunctionTileSourceCenter = new(55.75, 37.62);

    [field: MaybeNull, AllowNull]
    public TileSource FunctionTileSource => field ??= new FunctionalTileSource
    {
        TileFunc = async (tile, cancel) =>
        {
            var center = _FunctionTileSourceCenter;
            using var bmp = tile.CreatePixelAccessor();

            // Вариант 1: foreach с перечислителем
            foreach (var (point, location) in tile)
            {
                if (point.Y % 64 == 0)
                    cancel.ThrowIfCancellationRequested();

                var r = center.DistanceTo(location) / 1000.0;
                var f = r == 0 ? 1.0 : Math.Sin(r) / r;
                bmp[point] = HeatColor(Math.Max(0, f));
            }

            return bmp;

            static (byte R, byte G, byte B, byte A) HeatColor(double v, byte opacity = 200)
            {
                v = Math.Max(0, Math.Min(1, v));
                double r, g, b;
                if (v < 0.25)
                {
                    var t = v / 0.25;
                    r = 0; g = t * 255; b = 255;
                }
                else if (v < 0.5)
                {
                    var t = (v - 0.25) / 0.25;
                    r = 0; g = 255; b = (1 - t) * 255;
                }
                else if (v < 0.75)
                {
                    var t = (v - 0.5) / 0.25;
                    r = t * 255; g = 255; b = 0;
                }
                else
                {
                    var t = (v - 0.75) / 0.25;
                    r = 255; g = (1 - t) * 255; b = 0;
                }
                return ((byte)r, (byte)g, (byte)b, opacity);
            }
        }
    };

    private void UpdateLocationsListLength()
    {
        if (Locations is not { Count: > 0 and var count } locations)
        {
            LocationsListPathLength = 0;
            return;
        }

        var length = 0d;
        var start = locations[0];
        for (var i = 1; i < count; i++)
        {
            var point = locations[i];
            length += GPS.LengthBetween(
                new() { Latitude = start.Latitude, Longitude = start.Longitude },
                new GeoLocation { Latitude = point.Latitude, Longitude = point.Longitude });
            start = point;
        }

        LocationsListPathLength = length;
    }

    #region Command SelectMapCenterCommand : Location - Выбор положения

    /// <summary>Выбор положения</summary>
    private Command? _SelectMapCenterCommand;

    /// <summary>Выбор положения</summary>
    public ICommand SelectMapCenterCommand => _SelectMapCenterCommand ??= Command.New<Location>(p => MapCenter = p);

    #endregion

    #region Command RemoveLastPointCommand :  - Удаление последней точки

    /// <summary>Удаление последней точки</summary>
    private Command? _RemoveLastPointCommand;

    /// <summary>Удаление последней точки</summary>
    public ICommand RemoveLastPointCommand => _RemoveLastPointCommand ??= Command.New(
        _ =>
        {
            var selected = SelectedLocation;
            var last_location = Locations[^1];
            Locations.RemoveAt(Locations.Count - 1);
            if (selected == last_location && Locations.Count > 0)
                SelectedLocation = Locations[^1];
            UpdateLocationsListLength();
        },
        _ => Locations.Count > 0);

    #endregion

    #region Command SavePathCommand :  - Summary

    /// <summary>Summary</summary>
    private Command? _SavePathCommand;

    /// <summary>Summary</summary>
    public ICommand SavePathCommand => _SavePathCommand ??= Command.New<IEnumerable<Location>>(
        p =>
        {
            var dialog = new SaveFileDialog { Title = "Сохранить путь", Filter = "json (*.json)|*.json|Все файлы (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            using var file = dialog.OpenFile();
            JsonSerializer.Serialize(file, p);
        },
        l => l?.Any() == true);

    #endregion

    #region Command LoadPathCommand :  - Summary

    /// <summary>Summary</summary>
    private Command? _LoadPathCommand;

    /// <summary>Summary</summary>
    public ICommand LoadPathCommand => _LoadPathCommand ??= Command.New(
        _ =>
        {
            var dialog = new OpenFileDialog { Title = "Выбор файла", Filter = "json (*.json)|*.json|Все файлы (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;

            using var file = dialog.OpenFile();
            var locations = JsonSerializer.Deserialize<IEnumerable<Location>>(file);
            if (locations is null) return;

            var min_lat = double.PositiveInfinity;
            var min_lon = double.PositiveInfinity;

            var max_lat = double.NegativeInfinity;
            var max_lon = double.NegativeInfinity;

            var avg_lat = 0d;
            var avg_lon = 0d;

            var count = 0;

            Locations.Clear();
            foreach (var location in locations)
            {
                Locations.Add(location);

                var (lat, lon) = location;

                avg_lat += lat;
                avg_lon += lon;

                min_lat = Math.Min(min_lat, lat);
                min_lon = Math.Min(min_lon, lon);

                max_lat = Math.Min(max_lat, lat);
                max_lon = Math.Min(max_lon, lon);

                count++;
            }

            avg_lon /= count;
            avg_lat /= count;

            UpdateLocationsListLength();

            MapCenter = new(avg_lat, avg_lon);
        });

    #endregion

    public ICommand ResetFunctionLayer => field ??= Command.New(
        () =>
        {
            _FunctionTileSourceCenter = MapCenter ?? throw new InvalidOperationException();
            (FunctionTileSource as FunctionalTileSource)?.ResetLayer();
        });
}
