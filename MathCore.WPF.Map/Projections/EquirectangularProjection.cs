using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Равноугольная проекция X соответствует долготе, Y соответствует широте</summary>
public class EquirectangularProjection : MapProjection
{
    /// <summary>Создаёт проекцию с CRS по умолчанию EPSG:4326</summary>
    public EquirectangularProjection() : this("EPSG:4326") { }

    /// <summary>Создаёт проекцию с указанным идентификатором CRS</summary>
    /// <param name="CrsId">Идентификатор системы координат</param>
    public EquirectangularProjection(string CrsId) => this.CrsId = CrsId;

    /// <summary>Возвращает масштаб по осям в точке</summary>
    /// <param name="location">Географическая позиция</param>
    /// <returns>Масштаб по X и Y</returns>
    public override Point GetMapScale(Location location) => new(
            x: ViewportScale / (MetersPerDegree * Math.Cos(location.Latitude * Consts.ToRad)),
            y: ViewportScale / MetersPerDegree);

    /// <summary>Преобразует географическую позицию в декартовые координаты проекции</summary>
    /// <param name="location">Географическая позиция</param>
    /// <returns>Точка в системе координат проекции</returns>
    public override Point LocationToPoint(Location location) => new(location.Longitude, location.Latitude);

    /// <summary>Преобразует точку декартовой системы в географическую позицию</summary>
    /// <param name="point">Точка в системе координат проекции</param>
    /// <returns>Географическая позиция</returns>
    public override Location PointToLocation(Point point) => new(point.Y, point.X);

    /// <summary>Переводит позицию на экране в географические координаты как простое смещение</summary>
    /// <param name="location">Исходная позиция</param>
    /// <param name="translation">Смещение на экране</param>
    /// <returns>Новая позиция</returns>
    public override Location TranslateLocation(Location location, Point translation) => new(
             latitude: location.Latitude - translation.Y / ViewportScale,
            longitude: location.Longitude + translation.X / ViewportScale);
}