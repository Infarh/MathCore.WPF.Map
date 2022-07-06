using System.ComponentModel;
using System.Globalization;

namespace MathCore.WPF.Map.TileLayers;

public class TileSourceConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type SourceType) => SourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new TileSource { UriFormat = value as string };
}