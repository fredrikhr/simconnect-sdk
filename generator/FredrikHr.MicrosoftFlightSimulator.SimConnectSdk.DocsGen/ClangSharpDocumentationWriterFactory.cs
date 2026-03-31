using System.Xml;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed class ClangSharpDocumentationWriterFactory(
    IOptionsMonitor<XmlWriterSettings> xmlWriterOptions,
    ILoggerFactory? loggerFactory = default)
{
    private readonly ILoggerFactory loggerFactory =
        loggerFactory ?? Microsoft.Extensions.Logging.Abstractions
        .NullLoggerFactory.Instance;

    public ClangSharpDocumentationWriter CreateWriter(string xmlFilePath)
    {
        var logger = loggerFactory.CreateLogger<ClangSharpDocumentationWriter>();
        return new(xmlFilePath, xmlWriterOptions.Get(Options.DefaultName), logger);
    }
}
