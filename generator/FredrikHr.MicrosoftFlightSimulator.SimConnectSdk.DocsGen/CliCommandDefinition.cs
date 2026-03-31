using System.CommandLine;
using System.CommandLine.Hosting;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed class CliCommandDefinition : IHostedApplicationRootCommandDefinition<CliCommandDefinition>
{
    public static CliCommandDefinition Instance { get; } = new();

    public RootCommand RootCommand { get; }

    public CliCommandDefinition()
    {
        RootCommand = [];
        RootCommand.UseHostExecution<CliCommandExecution>(ConfigureHost);
    }

    public void ConfigureHost(HostApplicationBuilder hostBuilder)
    {
        if (hostBuilder is null) return;
        hostBuilder.Environment.ContentRootPath = Path.GetFullPath(
            hostBuilder.Environment.ContentRootPath
            );
        IServiceCollection services = hostBuilder.Services;
        services.AddSingleton<CookieContainer>();
        services.AddHttpClient<MsfsDocsWebsiteClient>()
            .ConfigureHttpClient(http =>
            {
                http.BaseAddress = MsfsDocsWebsiteClient.BaseAddress;
            })
            .ConfigurePrimaryHttpMessageHandler(MsfsDocsWebsiteClient.GetHttpMessageHandler);
        services.AddSingleton<MsfsDocsTableOfContentsProvider>();

        services.AddSingleton<XmlResolver>(new XmlPreloadedResolver(XmlKnownDtds.All));
        services.AddOptions<XmlReaderSettings>()
            .PostConfigure(static (XmlReaderSettings xmlSettings, XmlResolver xmlResolver) =>
            {
                xmlSettings.DtdProcessing = DtdProcessing.Parse;
                xmlSettings.XmlResolver = xmlResolver;
            });
        services.AddOptions<XmlWriterSettings>()
            .PostConfigure(static xmlSettings =>
            {
                xmlSettings.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                xmlSettings.Indent = true;
                xmlSettings.WriteEndDocumentOnClose = true;
            });
        services.AddSingleton<ClangSharpDocumentationWriterFactory>();

        services.AddSingleton<MsfsDocsReleaseNotesProvider>();

        services.Configure<ReverseMarkdown.Config>(static config =>
        {
            config.SmartHrefHandling = true;
        });
    }
}
