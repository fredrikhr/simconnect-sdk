using System.Xml;

using Microsoft.Extensions.Logging;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal partial class ClangSharpDocumentationWriter(
    string xmlFilePath,
    XmlWriterSettings xmlWriterSettings,
    ILogger<ClangSharpDocumentationWriter>? logger = null
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

    [LoggerMessage(LogLevel.Warning, $"Unexpected element with name <{{{nameof(childName)}}}>, child of element <{{{nameof(parentName)}}}>.")]
    private partial void LogUnexpectedChildElement(string childName, string parentName);
}
