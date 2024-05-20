using System.Xml;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class ClangSharpDocumentationWriter(
    string xmlFilePath,
    XmlWriterSettings xmlWriterSettings
    ) : IDisposable
{
    private readonly XmlWriter xmlWriter = CreateXmlWriter(xmlFilePath, xmlWriterSettings);

    private static XmlWriter CreateXmlWriter(string xmlFilePath, XmlWriterSettings settings)
    {
        var instSettings = settings.Clone();
        instSettings.CloseOutput = true;
        return XmlWriter.Create(xmlFilePath, instSettings);
    }

    public void Dispose()
    {
        xmlWriter.Dispose();
    }
}
