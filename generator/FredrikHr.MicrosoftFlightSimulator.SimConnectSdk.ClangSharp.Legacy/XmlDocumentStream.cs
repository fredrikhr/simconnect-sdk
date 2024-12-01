using System.Xml;
using System.Xml.XPath;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.ClangSharp;

internal class XmlDocumentStream(
    XmlReaderSettings? xmlSettings
    ) : MemoryStream()
{
    private XPathDocument? xmlDoc;

    public XPathDocument GetXmlDocument()
    {
        if (xmlDoc is null) CreateXmlDocument();
        return xmlDoc ?? throw new InvalidOperationException(
            "Unable to generate XML document from current state."
            );
    }

    public override void Close()
    {
        CreateXmlDocument(isClosing: true);
        base.Close();
    }

    private void CreateXmlDocument(bool isClosing = false)
    {
        long position = Position;
        Seek(0L, SeekOrigin.Begin);
        try
        {
            using var xmlReader = XmlReader.Create(
                this,
                xmlSettings
                );
            xmlDoc = new(xmlReader);
        }
        finally
        {
            if (!isClosing) Seek(position, SeekOrigin.Begin);
        }
    }
}
