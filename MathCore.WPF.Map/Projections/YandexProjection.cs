using System.Windows;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.Projections.Base;

namespace MathCore.WPF.Map.Projections;

/// <summary>Картографическая проекция для Яндекс.Карт, использующая модифицированную проекцию Меркатора</summary>
/// <example>
/// <code>
/// <![CDATA[
/// var projection = new YandexProjection();
/// var location = new Location(55.7558, 37.6173); // Москва
/// var point = projection.LocationToPoint(location);
/// var backLocation = projection.PointToLocation(point);
/// ]]>
/// </code>
/// </example>
public class YandexProjection : MapProjection
{
    /// <summary>Инициализирует новый экземпляр проекции Яндекс.Карт с идентификатором CRS по умолчанию</summary>
    public YandexProjection() : this("Yandex") { }

    /// <summary>Инициализирует новый экземпляр проекции Яндекс.Карт с указанным идентификатором системы координат</summary>
    /// <param name="CrsId">Идентификатор системы координат</param>
    public YandexProjection(string CrsId)
    {
        this.CrsId = CrsId;
        IsWebMercator = true;
        LongitudeScale = MetersPerDegree;
        MaxLatitude = YToLatitude(180d);
    }

    /// <summary>Вычисляет масштаб области просмотра для указанного уровня масштабирования</summary>
    /// <param name="ZoomLevel">Уровень масштабирования</param>
    /// <returns>Масштаб области просмотра</returns>
    public override double GetViewportScale(double ZoomLevel) => DegreesToViewportScale(ZoomLevel) / MetersPerDegree;

    /// <summary>Вычисляет масштаб карты для указанного местоположения с учетом искажения проекции Меркатора</summary>
    /// <param name="location">Местоположение на карте</param>
    /// <returns>Масштаб карты по осям X и Y</returns>
    public override Point GetMapScale(Location location)
    {
        var scale = ViewportScale / Math.Cos(location.Latitude * Consts.ToRad);

        return new(scale, scale);
    }

    /// <summary>Преобразует географические координаты в точку на карте</summary>
    /// <param name="location">Географические координаты</param>
    /// <returns>Точка на карте в проекции Яндекс</returns>
    public override Point LocationToPoint(Location location) => WGS84ToBing(location);

    /// <summary>Преобразует географические координаты WGS84 в координаты проекции Меркатора (Bing/Яндекс)</summary>
    /// <param name="coordinate">Географические координаты в системе WGS84</param>
    /// <returns>Точка в метрах в проекции Меркатора</returns>
    public static Point WGS84ToBing(Location coordinate)
    {
        var lon = coordinate.Longitude * Consts.ToRad;
        var lat = coordinate.Latitude * Consts.ToRad;
        const double e = 0.0818191908426;
        var f = e * Math.Sin(lat);
        var h = Math.Tan(Consts.PI025 + lat / 2);
        var j = Math.Pow(Math.Tan(Consts.PI025 + Math.Asin(f) / 2), e);
        var i = h / j;

        const double R = 6378137;
        return new(R * lon, R * Math.Log(i));
    }

    /// <summary>Преобразует точку на карте в географические координаты</summary>
    /// <param name="point">Точка на карте в проекции Яндекс</param>
    /// <returns>Географические координаты</returns>
    public override Location PointToLocation(Point point) => BingtoWGS84Mercator(point);

    /// <summary>Преобразует координаты проекции Меркатора (Bing/Яндекс) в географические координаты WGS84</summary>
    /// <param name="point">Точка в метрах в проекции Меркатора</param>
    /// <returns>Географические координаты в системе WGS84</returns>
    public static Location BingtoWGS84Mercator(Point point)
    {
        const double k = 180 / 20037508.34;
        var lat = point.Y * k;
        var lon = point.X * k;

        lat = 2 * Consts.ToDeg * Math.Atan(Math.Exp(lat * Consts.ToRad)) - 90;

        return new(lat, lon);
    }

    /// <summary>Выполняет перемещение местоположения на указанное смещение с учетом масштаба и искажения проекции</summary>
    /// <param name="location">Исходное местоположение</param>
    /// <param name="translation">Смещение в пикселях области просмотра</param>
    /// <returns>Новое местоположение после применения смещения</returns>
    public override Location TranslateLocation(Location location, Point translation)
    {
        var scale_x = MetersPerDegree * ViewportScale;
        var scale_y = scale_x / Math.Cos(location.Latitude * Consts.ToRad);

        return new(
            latitude: location.Latitude - translation.Y / scale_y,
            longitude: location.Longitude + translation.X / scale_x);
    }

    /// <summary>Вычисляет широту по Y-координате в проекции Меркатора с итерационным уточнением для учета эксцентриситета Земли</summary>
    /// <param name="y">Y-координата в градусах</param>
    /// <returns>Широта в градусах</returns>
    public static double YToLatitude(double y)
    {
        var e = Math.Exp(-y * Consts.ToRad); // p.44 (7-10)
        var lat = Consts.PI05 - 2 * Math.Atan(e); // p.44 (7-11)

        const double accuracy = 1e-6;
        const int max_iterations = 10;
        for (var (i, delta)= (0, 1d); delta > accuracy && i < max_iterations; i++)
        {
            var new_lat = Consts.PI05 - 2 * Math.Atan(e * ConformalFactor(lat)); // p.44 (7-9)
            (lat, delta) = (new_lat, Math.Abs(1 - new_lat / lat));
        }

        static double ConformalFactor(double lat)
        {
            var sin_lat = Eccentricity * Math.Sin(lat);
            return Math.Pow((1d - sin_lat) / (1d + sin_lat), Eccentricity / 2d);
        }

        return lat * Consts.ToDeg;
    }
}
