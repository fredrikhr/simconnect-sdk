using System.Xml;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class ClangSharpDocumentationWriterFactory(
    IOptions<XmlWriterSettings> xmlWriterOptions,
    ILoggerFactory? loggerFactory)
{
    private readonly ILoggerFactory loggerFactory =
        loggerFactory ?? Microsoft.Extensions.Logging.Abstractions
        .NullLoggerFactory.Instance;

    public ClangSharpDocumentationWriter CreateWriter(string xmlFilePath)
    {
        var logger = loggerFactory.CreateLogger<ClangSharpDocumentationWriter>();
        return new(xmlFilePath, xmlWriterOptions.Value, logger);
    }
}
