using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MathCore.WPF.Map.Infrastructure;

internal static class MatrixEx
{
    /// <summary>Формирование матрицы трансформации изображения для объектов на карте</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix TranslateScaleRotateTranslate(
        double Translation1X,
        double Translation1Y,
        double ScaleX,
        double ScaleY,
        double RotationAngle,
        double Translation2X,
        double Translation2Y)
    {
        var matrix = new Matrix(1d, 0d, 0d, 1d, Translation1X, Translation1Y);
        matrix.Scale(ScaleX, ScaleY);
        matrix.Rotate(RotationAngle);
        matrix.Translate(Translation2X, Translation2Y);
        return matrix;
    }
}