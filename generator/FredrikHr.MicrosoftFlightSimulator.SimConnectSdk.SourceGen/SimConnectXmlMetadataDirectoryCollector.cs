using System.Xml;
using System.Xml.Serialization;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ClangSharp.XmlMetadata;
using Microsoft.Extensions.FileProviders;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.SourceGen;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812: Avoid uninstantiated internal classes",
    Justification = nameof(Microsoft.Extensions.DependencyInjection)
    )]
internal sealed partial class SimConnectXmlMetadataDirectoryCollector(
    IHostEnvironment env,
    IHostApplicationLifetime appLifetime,
    XmlSerializerFactory xmlFactory,
    IOptions<XmlReaderSettings> xmlOptions,
    ILogger<SimConnectXmlMetadataDirectoryCollector> logger
    )
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1823: Avoid unused private fields",
        Justification = nameof(LoggerMessageAttribute)
        )]
    private readonly ILogger logger = logger ?? Microsoft.Extensions.Logging.Abstractions
        .NullLogger<SimConnectXmlMetadataDirectoryCollector>.Instance;
    private readonly XmlSerializer xmlSerializer = xmlFactory
        .CreateSerializer(typeof(PInvokeBindings));
    private readonly XmlReaderSettings xmlSettings = xmlOptions.Value;
    private string @namespace = default!;
    private readonly Dictionary<string, PInvokeClass> classes =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, PInvokeStruct> structs =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, PInvokeEnumeration> enums =
        new(StringComparer.Ordinal);

    public void Execute()
    {
        IEnumerable<IFileInfo> files = env.ContentRootFileProvider
            .GetDirectoryContents(string.Empty)
            .Where(IsXmlFile);

        ParallelOptions parallelOptions = new()
        { CancellationToken = appLifetime.ApplicationStopping };
        if (env.IsDevelopment()) parallelOptions.MaxDegreeOfParallelism = 1;
        ParallelLoopResult collectResult = Parallel.ForEach(
            files,
            parallelOptions,
            CollectXmlFile
            );
        if (collectResult.IsCompleted)
            LogCollectionCompletedSuccessfully(@namespace, structs.Count, enums.Count, classes.Count);
        else
        {
            LogCollectionIncomplete();
            throw new OperationCanceledException(appLifetime.ApplicationStopping);
        }

        static bool IsXmlFile(IFileInfo dirEntry)
        {
            return !dirEntry.IsDirectory
                && string.Equals(
                    ".xml",
                    Path.GetExtension(dirEntry.Name),
                    StringComparison.OrdinalIgnoreCase
                    );
        }
    }

    private void CollectXmlFile(IFileInfo xmlFileInfo)
    {
        LogCollectingFile(xmlFileInfo.Name);
        using XmlReader xmlReader = XmlReader.Create(
            xmlFileInfo.CreateReadStream(),
            xmlSettings
            );
        var bindings = (PInvokeBindings)
            xmlSerializer.Deserialize(xmlReader)!;
        PInvokeNamespace @namespace = bindings.Namespace;
        this.@namespace = @namespace.Name;
        foreach (PInvokeClass @class in @namespace.Classes ?? [])
            classes.Add(@class.Name, @class);
        foreach (PInvokeStruct @struct in @namespace.Structs ?? [])
            structs.Add(@struct.Name, @struct);
        foreach (PInvokeEnumeration @enum in @namespace.Enums ?? [])
            enums.Add(@enum.Name, @enum);
    }

    [LoggerMessage(LogLevel.Debug, $"Collecting {{{nameof(fileName)}}}")]
    private partial void LogCollectingFile(string fileName);

    [LoggerMessage(LogLevel.Information, $"Collecting files complete, Namespace = {{{nameof(@namespace)}}}, Structs = {{{nameof(structs)}}}, Enums = {{{nameof(enums)}}}, Classes = {{{nameof(classes)}}}")]
    private partial void LogCollectionCompletedSuccessfully(string @namespace, int structs, int enums, int classes);

    [LoggerMessage(LogLevel.Error, $"")]
    private partial void LogCollectionIncomplete();
}
