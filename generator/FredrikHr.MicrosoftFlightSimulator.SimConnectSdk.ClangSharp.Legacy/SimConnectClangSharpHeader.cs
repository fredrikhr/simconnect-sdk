using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.ClangSharp;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812: Avoid uninstantiated internal classes",
    Justification = nameof(Microsoft.Extensions.DependencyInjection)
    )]
internal sealed partial class SimConnectClangSharpHeader
{
    private static readonly string[] prependLines = [
        """#include <Windows.h>"""
        ];

    private readonly ILogger<SimConnectClangSharpHeader> logger;

    public SimConnectClangSharpHeader(
        IHostEnvironment environment,
        ILogger<SimConnectClangSharpHeader>? logger = null
        ) : base()
    {
        this.logger = logger ?? Microsoft.Extensions.Logging.Abstractions
            .NullLogger<SimConnectClangSharpHeader>.Instance;

        string msfsSdkPath = Environment.GetEnvironmentVariable("MSFS_SDK")
            ?? throw new InvalidOperationException("Environment variable MSFS_SDK is not defined");
        LogMsfsSdk(msfsSdkPath);
        string sourceSimConnectPath = Path.Combine(
            msfsSdkPath,
            "SimConnect SDK",
            "include",
            "SimConnect.h"
            );
        string[] simConnectHeaderLines = File.ReadAllLines(sourceSimConnectPath);

        string directoryPath = environment.ContentRootPath;
        string filePath = Path.Combine(directoryPath, "SimConnect.h");
        LogCreatingHeaderFile(filePath);
        Directory.CreateDirectory(directoryPath);
        File.WriteAllLines(filePath, prependLines);
        File.AppendAllLines(filePath, simConnectHeaderLines);

        FilePath = filePath;
    }

    public string FilePath { get; }

    [LoggerMessage(LogLevel.Information, $"MSFS_SDK: {{{nameof(msfsSdkPath)}}}")]
    private partial void LogMsfsSdk(string msfsSdkPath);

    [LoggerMessage(LogLevel.Information, $"Creating header file: {{{nameof(headerFilePath)}}}")]
    private partial void LogCreatingHeaderFile(string headerFilePath);
}
