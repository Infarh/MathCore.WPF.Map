using System.ComponentModel;
using System.Globalization;

namespace MathCore.WPF.Map.Primitives;

public class BoundingBoxConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type SourceType) => SourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => BoundingBox.Parse((string)value);
}