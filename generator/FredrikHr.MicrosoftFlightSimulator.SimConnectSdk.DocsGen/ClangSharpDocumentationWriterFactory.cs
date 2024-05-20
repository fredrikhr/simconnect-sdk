using System.Xml;

using Microsoft.Extensions.Options;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class ClangSharpDocumentationWriterFactory(
    IOptions<XmlWriterSettings> xmlWriterOptions)
{
    public ClangSharpDocumentationWriter CreateWriter(string xmlFilePath) =>
        new(xmlFilePath, xmlWriterOptions.Value);
}
