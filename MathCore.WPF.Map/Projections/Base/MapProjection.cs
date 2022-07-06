using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

using MathCore.WPF.Map.Infrastructure;
using MathCore.WPF.Map.Primitives;
using MathCore.WPF.Map.Primitives.Base;
using MathCore.WPF.Map.TileLayers;

namespace MathCore.WPF.Map.Projections.Base;

/// <summary>Преобразователь географических координат в экранные координаты карты</summary>
[Serializable]
[TypeConverter(typeof(MapProjectionConverter))]
public abstract class MapProjection
{
    /// <summary>Размер тайла в пикселях</summary>
    public const int TileSize = 256;

    /// <summary>Масштаб тайла в пикселях на один градус</summary>
    public const double TileSizeScale = TileSize / 360d;

    /// <summary>Экваториальный радиус Земли</summary>
    public const double Wgs84EquatorialRadius = 6378137d;

    /// <summary>Метров на градус замной поверхности</summary>
    public const double MetersPerDegree = Wgs84EquatorialRadius * Consts.ToRad;

    public const double Flattening = 1d / 298.257223563;

    /// <summary>Эксцентриситет эллипса</summary>
    public static readonly double Eccentricity = Math.Sqrt((2d - Flattening) * Flattening);

    /// <summary>
    /// Возвращает коэффициент масштабирования из декартовых координат карты в градусах
    /// в координаты экрана для указанного уровня масштабирования.
    /// </summary>
    public static double DegreesToViewportScale(double ZoomLevel) => Math.Pow(2d, ZoomLevel) * TileSizeScale;

    /// <summary>Идентификатор вида проекции</summary>
    public string CrsId { get; set; }

    /// <summary>Является ли проекция поддерживаемой проекцией веб-меркатора, поддерживаемая <see cref="MapTileLayer"/></summary>
    public bool IsWebMercator { get; protected set; }

    /// <summary>Является ли проекция азимутальной</summary>
    public bool IsAzimuthal { get; protected set; }

    /// <summary>
    /// Возвращает масштабный коэффициент из долготы в значение x нормальной цилиндрической проекции карты.
    /// Возвращает <c>NaN</c>, если это не обычная цилиндрическая проекция.
    /// </summary>
    public double LongitudeScale { get; protected set; } = 1d;

    /// <summary>Максимальное значение широты, поддерживаемое данной проекцией</summary>
    public double MaxLatitude { get; protected set; } = 90d;

    /// <summary>Коэффициент масштабирования из декартовых координат карты в координаты экрана</summary>
    public double ViewportScale { get; protected set; }

    /// <summary>Преобразование из декартовых координат карты в координаты экрана</summary>
    public MatrixTransform ViewportTransform { get; } = new();

    /// <summary>Коэффициент масштабирования из декартовых координат карты в координаты экрана для указанного уровня масштабирования</summary>
    public virtual double GetViewportScale(double ZoomLevel) => DegreesToViewportScale(ZoomLevel);

    /// <summary>Масштаб карты в указанном местоположении в единицах координат экрана на метр (пикселей/м)</summary>
    public abstract Point GetMapScale(Location location);

    /// <summary>Преобразование местоположения в географических координатах в точку в декартовых координатах карты</summary>
    public abstract Point LocationToPoint(Location location);

    /// <summary>Преобразование точки в декартовых координатах карты в местоположение в географических координатах</summary>
    public abstract Location PointToLocation(Point point);

    /// <summary>Перемещение положения <see cref="Location"/> в географических координатах на указанную небольшую величину в экранных координатах</summary>
    public abstract Location TranslateLocation(Location location, Point translation);

    /// <summary>Преобразование области географических координат в область декартовых координат экрана карты</summary>
    public virtual Rect BoundingBoxToRect(BoundingBox BoundingBox) => new(
        point1: LocationToPoint(new(BoundingBox.South, BoundingBox.West)),
        point2: LocationToPoint(new(BoundingBox.North, BoundingBox.East)));

    /// <summary>Преобразование области экранных координат карты в область географических координат</summary>
    public virtual BoundingBox RectToBoundingBox(Rect rect)
    {
        var sw = PointToLocation(new(rect.X, rect.Y));
        var ne = PointToLocation(new(rect.X + rect.Width, rect.Y + rect.Height));

        return new(
            north: ne.Latitude,
            east: ne.Longitude,
            south: sw.Latitude,
            west: sw.Longitude);
    }

    /// <summary>Преобразование точки в географических координатах в экранные координаты карты</summary>
    public Point LocationToViewportPoint(Location location) => ViewportTransform.Transform(LocationToPoint(location));

    /// <summary>Преобразование точки в экранных координатах карты в точку в току в географических координатах</summary>
    public Location ViewportPointToLocation(Point point) => PointToLocation(ViewportTransform.Inverse!.Transform(point));

    /// <summary>Преобразование области экранных координат в область географических координат</summary>
    public BoundingBox ViewportRectToBoundingBox(Rect rect) => RectToBoundingBox(ViewportTransform.Inverse!.TransformBounds(rect));

    /// <summary>Установка масштаба в экранных координатах в трансформацию карты</summary>
    public virtual void SetViewportTransform(Location Center, Location MapCenter, Point ViewportCenter, double ZoomLevel, double hHeading)
    {
        ViewportScale = GetViewportScale(ZoomLevel);

        var center = LocationToPoint(MapCenter);

        ViewportTransform.Matrix = MatrixEx.TranslateScaleRotateTranslate(
            Translation1X: -center.X,
            Translation1Y: -center.Y,
            ScaleX: ViewportScale,
            ScaleY: -ViewportScale,
            RotationAngle: hHeading,
            Translation2X: ViewportCenter.X,
            Translation2Y: ViewportCenter.Y);
    }

    /// <summary>Формирование параметров запроса WMS 1.3.0 таких как "CRS=...&BBOX=...&WIDTH=...&HEIGHT=..."</summary>
    public virtual string WmsQueryParameters(BoundingBox BoundingBox, string Version = "1.3.0")
    {
        if (CrsId is not { Length: > 0 } crs_id)
            return null;

        string format;

        if (Version.StartsWith("1.1."))
            format = "SRS={0}&BBOX={1},{2},{3},{4}&WIDTH={5}&HEIGHT={6}";
        else if (crs_id == "EPSG:4326")
            format = "CRS={0}&BBOX={2},{1},{4},{3}&WIDTH={5}&HEIGHT={6}";
        else
            format = "CRS={0}&BBOX={1},{2},{3},{4}&WIDTH={5}&HEIGHT={6}";

        var rect = BoundingBoxToRect(BoundingBox);
        var width = (int)Math.Round(ViewportScale * rect.Width);
        var height = (int)Math.Round(ViewportScale * rect.Height);

        return string.Format(CultureInfo.InvariantCulture, format, crs_id,
            rect.X,
            rect.Y,
            rect.X + rect.Width,
            rect.Y + rect.Height,
            width,
            height);
    }
}