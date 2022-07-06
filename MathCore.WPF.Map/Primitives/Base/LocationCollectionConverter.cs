using System.ComponentModel;
using System.Globalization;

namespace MathCore.WPF.Map.Primitives.Base;

public class LocationCollectionConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type SourceType) => SourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => LocationCollection.Parse((string)value);
}