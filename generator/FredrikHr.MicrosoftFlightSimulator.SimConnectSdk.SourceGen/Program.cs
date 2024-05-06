using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.CommandLine.NamingConventionBinder;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Resolvers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.SourceGen;
using System.CommandLine.Invocation;

RootCommand cliCommand = new()
{
    Handler = CommandHandler.Create<IHost, CancellationToken>(RunCli),
};
Option<DirectoryInfo> sourceDirCliOption = new("--source-directory");
sourceDirCliOption.ExistingOnly();
sourceDirCliOption.AddAlias("--source-dir");
sourceDirCliOption.AddAlias("-s");
sourceDirCliOption.IsRequired = true;
cliCommand.AddOption(sourceDirCliOption);

CommandLineBuilder cliBuilder = new(cliCommand);
cliBuilder.UseDefaults();
cliBuilder.UseHost(Host.CreateDefaultBuilder, host =>
{
    InvocationContext context = host.GetInvocationContext();
    ParseResult parseResult = context.ParseResult;
    string sourceDirectory = parseResult
        .GetValueForOption(sourceDirCliOption)!
        .FullName;
    host.UseContentRoot(sourceDirectory);
    host.ConfigureServices(ConfigureServices);
});
return await cliBuilder.Build().InvokeAsync(args)
    .ConfigureAwait(continueOnCapturedContext: false);

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

static Task RunCli(IHost host, CancellationToken cancelToken)
{
    var appLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    using IDisposable cancelReg = cancelToken.Register(
        static s => ((IHostApplicationLifetime)s!).StopApplication(),
        appLifetime
        );
    var collector = host.Services.GetRequiredService<SimConnectXmlMetadataDirectoryCollector>();
    collector.Execute();
    appLifetime.StopApplication();
    return host.WaitForShutdownAsync(cancelToken);
}
