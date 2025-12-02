using System.Globalization;
using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Primitives.Base;

namespace MathCore.WPF.Map.Projections.Base;

/// <summary>Азимутальная проекция</summary>
public abstract class AzimuthalProjection : MapProjection
{
    /// <summary>Центр проекции</summary>
    public Location ProjectionCenter { get; private set; } = new();

    /// <summary>Создаёт экземпляр азимутальной проекции и настраивает параметры</summary>
    protected AzimuthalProjection()
    {
        IsAzimuthal = true;
        LongitudeScale = double.NaN;
    }

    /// <summary>Возвращает масштаб видимой области как отношение градусов к пикселям</summary>
    /// <param name="ZoomLevel">Уровень масштаба</param>
    /// <returns>Масштаб видимой области</returns>
    public override double GetViewportScale(double ZoomLevel) => DegreesToViewportScale(ZoomLevel) / MetersPerDegree;

    /// <summary>Возвращает масштаб карты по обеим осям</summary>
    /// <param name="location">Географическая позиция</param>
    /// <returns>Масштаб по X и Y</returns>
    public override Point GetMapScale(Location location) => new(ViewportScale, ViewportScale);

    /// <summary>Переводит географическую позицию с учётом экранного смещения</summary>
    /// <param name="location">Исходная позиция</param>
    /// <param name="translation">Смещение на экране в пикселях</param>
    /// <returns>Новая географическая позиция</returns>
    public override Location TranslateLocation(Location location, Point translation)
    {
        var scale_y = ViewportScale * MetersPerDegree;
        var scale_x = scale_y * Math.Cos(location.Latitude * Consts.ToRad);

        return new(
            latitude: location.Latitude - translation.Y / scale_y,
            longitude: location.Longitude + translation.X / scale_x);
    }

    /// <summary>Преобразует ограничивающий прямоугольник координат в декартовую систему проекции</summary>
    /// <param name="BoundingBox">Границы области</param>
    /// <returns>Прямоугольник в декартовых координатах проекции</returns>
    public override Rect BoundingBoxToRect(BoundingBox BoundingBox)
    {
        var center = LocationToPoint(BoundingBox.GetCenter());

        return new(
            x: center.X - BoundingBox.Width / 2,
            y: center.Y - BoundingBox.Height / 2,
            width: BoundingBox.Width,
            height: BoundingBox.Height);
    }

    /// <summary>Преобразует прямоугольник проекции в географические границы</summary>
    /// <param name="rect">Прямоугольник в декартовых координатах</param>
    /// <returns>Географические границы области</returns>
    public override BoundingBox RectToBoundingBox(Rect rect)
    {
        var center = PointToLocation(new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));

        return new(center, rect.Width, rect.Height); // width и height в метрах
    }

    /// <summary>Устанавливает трансформацию видимой области и центр проекции</summary>
    /// <param name="Center">Центр видимой области</param>
    /// <param name="MapCenter">Центр карты</param>
    /// <param name="ViewportCenter">Центр экрана</param>
    /// <param name="ZoomLevel">Уровень масштаба</param>
    /// <param name="Heading">Азимут поворота</param>
    public override void SetViewportTransform(Location Center, Location MapCenter, Point ViewportCenter, double ZoomLevel, double Heading)
    {
        ProjectionCenter = Center;

        base.SetViewportTransform(Center, MapCenter, ViewportCenter, ZoomLevel, Heading);
    }

    /// <summary>Формирует параметры запроса WMS для указанной области</summary>
    /// <param name="BoundingBox">Границы области</param>
    /// <param name="Version">Версия протокола WMS</param>
    /// <returns>Строка параметров запроса или null если `CrsId` не задан</returns>
    public override string? WmsQueryParameters(BoundingBox BoundingBox, string Version = "1.3.0")
    {
        if (string.IsNullOrEmpty(CrsId))
            return null;

        var rect = BoundingBoxToRect(BoundingBox);
        var width = (int)Math.Round(ViewportScale * rect.Width);
        var height = (int)Math.Round(ViewportScale * rect.Height);
        var crs = Version.StartsWith("1.1.") ? "SRS" : "CRS";

        return string.Format(CultureInfo.InvariantCulture,
            "{0}={1},1,{2},{3}&BBOX={4},{5},{6},{7}&WIDTH={8}&HEIGHT={9}",
            crs,
            CrsId,
            ProjectionCenter.Longitude,
            ProjectionCenter.Latitude,
            rect.X,
            rect.Y,
            rect.X + rect.Width,
            rect.Y + rect.Height,
            width,
            height);
    }

    /// <summary>Расчёт азимута и дистанции в радианах между двумя позициями</summary>
    /// <param name="location1">Первая позиция</param>
    /// <param name="location2">Вторая позиция</param>
    /// <returns>Азимут и дистанция в радианах</returns>
    public static (double Azimuth, double Distance) GetAzimuthDistance(Location location1, Location location2)
    {
        var lat1 = location1.Latitude * Consts.ToRad;
        var lon1 = location1.Longitude * Consts.ToRad;
        var lat2 = location2.Latitude * Consts.ToRad;
        var lon2 = location2.Longitude * Consts.ToRad;
#if NET8_0_OR_GREATER
        var (sin_lat1, cos_lat1) = Math.SinCos(lat1);
        var (sin_lat2, cos_lat2) = Math.SinCos(lat2);
        var (sin_lon12, cos_lon12) = Math.SinCos(lon2 - lon1);
#else
        var cos_lat1 = Math.Cos(lat1);
        var sin_lat1 = Math.Sin(lat1);
        var cos_lat2 = Math.Cos(lat2);
        var sin_lat2 = Math.Sin(lat2);
        var sin_lon12 = Math.Sin(lon2 - lon1);
        var cos_lon12 = Math.Cos(lon2 - lon1);
#endif
        var cos_distance = sin_lat1 * sin_lat2 + cos_lat1 * cos_lat2 * cos_lon12;

        var azimuth = Math.Atan2(sin_lon12, cos_lat1 * sin_lat2 / cos_lat2 - sin_lat1 * cos_lon12);
        var distance = Math.Acos(Math.Max(Math.Min(cos_distance, 1), -1));

        return (azimuth, distance);
    }

    /// <summary>Расчёт позиции по исходной позиции, азимуту и расстоянию</summary>
    /// <param name="location">Исходная позиция</param>
    /// <param name="azimuth">Азимут в радианах</param>
    /// <param name="distance">Расстояние в радианах</param>
    /// <returns>Новая позиция</returns>
    public static Location GetLocation(Location location, double azimuth, double distance)
    {
        var lat1 = location.Latitude * Consts.ToRad;
#if NET8_0_OR_GREATER
        var (sin_distance, cos_distance) = Math.SinCos(distance);
        var (sin_azimuth, cos_azimuth) = Math.SinCos(azimuth);
        var (sin_lat1, cos_lat1) = Math.SinCos(lat1);
#else
        var sin_distance = Math.Sin(distance);
        var cos_distance = Math.Cos(distance);
        var cos_azimuth = Math.Cos(azimuth);
        var sin_azimuth = Math.Sin(azimuth);
        var cos_lat1 = Math.Cos(lat1);
        var sin_lat1 = Math.Sin(lat1); 
#endif
        var sin_lat2 = sin_lat1 * cos_distance + cos_lat1 * sin_distance * cos_azimuth;
        var lat2 = Math.Asin(Math.Max(Math.Min(sin_lat2, 1), -1));
        var d_lon = Math.Atan2(sin_distance * sin_azimuth, cos_lat1 * cos_distance - sin_lat1 * sin_distance * cos_azimuth);

        return new(lat2 * Consts.ToDeg, location.Longitude + d_lon * Consts.ToDeg);
    }
}