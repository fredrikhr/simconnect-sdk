using System.Xml;
using System.Xml.Resolvers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ClangSharp;

using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.ClangSharp;

HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(args);
appBuilder.Environment.ContentRootPath = Path.GetFullPath(
    appBuilder.Environment.ContentRootPath
    );
appBuilder.Services.AddTransient<SimConnectClangSharpHeader>();
appBuilder.Services.AddSingleton<SimConnectPInvokeGenerator>();
appBuilder.Services.AddSingleton(new PInvokeGeneratorConfiguration(
    language: "c++",
    languageStandard: "",
    defaultNamespace: "FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.Interop",
    outputLocation: appBuilder.Environment.ContentRootPath,
    headerFile: null,
    PInvokeGeneratorOutputMode.Xml,
    PInvokeGeneratorConfigurationOptions.None
    | PInvokeGeneratorConfigurationOptions.GenerateLatestCode
    | PInvokeGeneratorConfigurationOptions.GenerateMultipleFiles
    | PInvokeGeneratorConfigurationOptions.GenerateMacroBindings
    | PInvokeGeneratorConfigurationOptions.GenerateNativeInheritanceAttribute
    )
{
    DefaultClass = "SimConnectNativeMethods",
    LibraryPath = "SimConnect.dll",
    RemappedNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["char"] = "byte",
        ["DWORD"] = "int",
        ["GUID"] = "Guid",
        ["Handle"] = "nint",
        ["HWND"] = "nint",
        ["SIMCONNECT_STRING"] = "SIMCONNECT_STRING",
        ["SIMCONNECT_STRINGV"] = "SIMCONNECT_STRINGV",
    },
});
appBuilder.Services.AddSingleton<XmlResolver>(new XmlPreloadedResolver(XmlKnownDtds.All));
appBuilder.Services.AddOptions<XmlReaderSettings>()
    .Configure((XmlReaderSettings xmlSettings, XmlResolver xmlResolver) =>
    {
        xmlSettings.IgnoreWhitespace = true;
        xmlSettings.DtdProcessing = DtdProcessing.Parse;
        xmlSettings.XmlResolver = xmlResolver;
    });

using var host = appBuilder.Build();
await host.StartAsync()
    .ConfigureAwait(continueOnCapturedContext: false);

try
{
    var generator = host.Services.GetRequiredService<SimConnectPInvokeGenerator>();
    generator.Execute();
}
finally
{
    await host.StopAsync()
        .ConfigureAwait(continueOnCapturedContext: false);
}
