using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Resolvers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.SourceGen;

Option<DirectoryInfo> sourceDirCliOption = new(
    "--source-directory",
    "-s"
    )
{
    Required = true,
};
sourceDirCliOption.AcceptExistingOnly();
RootCommand cliCommand = new()
{
    Options = { sourceDirCliOption },
};
cliCommand.UseHostExecution<SimConnectXmlMetadataDirectoryCollector>(
    CreateHostBuilder,
    static host => host.ConfigureServices(ConfigureServices)
    );
await cliCommand.Parse(args).InvokeAsync()
    .ConfigureAwait(continueOnCapturedContext: false);

IHostBuilder CreateHostBuilder(string[] builderArgs, ParseResult parseResult)
{
    IHostBuilder hostBuilder = Host.CreateDefaultBuilder(builderArgs);
    if (parseResult.GetResult(sourceDirCliOption) is OptionResult sourceDirResult &&
        sourceDirResult.GetValueOrDefault<DirectoryInfo>() is DirectoryInfo sourceDirInfo
        )
    {
        hostBuilder.UseContentRoot(sourceDirInfo.FullName);
    }
    return hostBuilder;
}

static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<XmlSerializerFactory>();
    services.AddSingleton<XmlResolver>(new XmlPreloadedResolver(XmlKnownDtds.All));
    services.AddOptions<XmlReaderSettings>()
        .BindConfiguration(nameof(XmlReader))
        .PostConfigure((XmlReaderSettings settings, XmlResolver xmlResolver) =>
        {
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.XmlResolver = xmlResolver;
        });

    services.AddSingleton<SimConnectXmlMetadataDirectoryCollector>();
}
