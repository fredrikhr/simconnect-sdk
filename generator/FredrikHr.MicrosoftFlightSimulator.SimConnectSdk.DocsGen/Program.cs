using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;

using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(args);
appBuilder.Environment.ContentRootPath = Path.GetFullPath(appBuilder.Environment.ContentRootPath);

appBuilder.Services.AddSingleton<CookieContainer>();
appBuilder.Services.AddHttpClient<MsfsDocsWebsiteClient>()
    .ConfigureHttpClient(http =>
    {
        http.BaseAddress = MsfsDocsWebsiteClient.BaseAddress;
    })
    .ConfigurePrimaryHttpMessageHandler(MsfsDocsWebsiteClient.GetHttpMessageHandler);
appBuilder.Services.AddSingleton<MsfsDocsTableOfContentsProvider>();

appBuilder.Services.AddSingleton<XmlResolver>(new XmlPreloadedResolver(XmlKnownDtds.All));
appBuilder.Services.AddOptions<XmlReaderSettings>()
    .PostConfigure(static (XmlReaderSettings xmlSettings, XmlResolver xmlResolver) =>
    {
        xmlSettings.DtdProcessing = DtdProcessing.Parse;
        xmlSettings.XmlResolver = xmlResolver;
    });
appBuilder.Services.AddOptions<XmlWriterSettings>()
    .PostConfigure(static (XmlWriterSettings xmlSettings) =>
    {
        xmlSettings.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        xmlSettings.Indent = true;
        xmlSettings.WriteEndDocumentOnClose = true;
    });
appBuilder.Services.AddSingleton<ClangSharpDocumentationWriterFactory>();

using IHost appHost = appBuilder.Build();
await appHost.RunAsync()
    .ConfigureAwait(continueOnCapturedContext: false);
