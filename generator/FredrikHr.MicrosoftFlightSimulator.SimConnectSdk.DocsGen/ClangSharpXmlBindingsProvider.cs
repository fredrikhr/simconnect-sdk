using Microsoft.Extensions.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class ClangSharpXmlBindingsProvider(
    IHostEnvironment environment
    )
{
    public string XmlBindingsDirectoryPath { get; } = Path.GetFullPath(
        Path.Combine(environment.ContentRootPath, "Xml")
        );


}
