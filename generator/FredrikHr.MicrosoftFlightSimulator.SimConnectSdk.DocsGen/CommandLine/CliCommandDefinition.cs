using System.CommandLine;
using System.CommandLine.Hosting;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen.CommandLine;

internal sealed class CliCommandDefinition : IHostedApplicationRootCommandDefinition<CliCommandDefinition>
{
    public static CliCommandDefinition Instance { get; } = new();

    public RootCommand RootCommand { get; }

    public Option<MsfsVariantTarget> TargetOption { get; }

    private CliCommandDefinition()
    {
        TargetOption = new("--target", "-t")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Microsoft Flight Simulator variant to generate",
            DefaultValueFactory = static (_) => MsfsVariantTarget.MSFS,
        };
        TargetOption.Configure<CliOptions, MsfsVariantTarget>(
            (opts, value) => opts.Target = value
            );
        RootCommand = new()
        {
            Description = "Generates Release notes, licesense information and .NET XML documentation comments for Microsoft Flight Simulator using the SDK documentation web portal.",
            Options = { TargetOption },
        };
        RootCommand.UseHostExecution<CliCommandExecution>(ConfigureHost);
    }

    public void ConfigureHost(HostApplicationBuilder hostBuilder)
    {
        if (hostBuilder is null) return;
        hostBuilder.Environment.ContentRootPath = Path.GetFullPath(
            hostBuilder.Environment.ContentRootPath
            );
        IServiceCollection services = hostBuilder.Services;
        services.AddOptions<CliOptions>()
            .BindConfiguration(nameof(CliOptions));
        services.AddSingleton<CookieContainer>();
        services.AddHttpClient<MsfsDocsWebsiteClient>()
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
        services.AddSingleton<MsfsDocsReleaseNotesEmitter>();
    }
}
