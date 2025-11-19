using System.Xml.Linq;

namespace MathCore.WPF.Map.Infrastructure;

internal static class XDocumentAsync
{
    public static Task<XDocument> LoadFromUriAsync(Uri uri) => Task
       .Factory
       .StartNew(v => XDocument.Load((string)v!), uri.ToString());
}
