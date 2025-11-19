namespace MathCore.WPF.Map.Infrastructure;

internal class XmlDocument : System.Xml.XmlDocument
{
    public static Task<XmlDocument> LoadFromUriAsync(Uri uri) => Task.Factory.StartNew(o =>
    {
        var document = new XmlDocument();
        document.Load(((Uri)o!).ToString());
        return document;
    }, uri);
}