using System.CommandLine.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen.CommandLine;

internal sealed class CliCommandExecution(
    MsfsDocsReleaseNotesProvider releaseNotesProvider,
    MsfsDocsReleaseNotesEmitter releaseNotesEmitter
    ) : IHostedCommandExecution
{
    public async Task<int> InvokeAsync(CancellationToken cancelToken = default)
    {
        Dictionary<Version, string> releseNotesVersions = await releaseNotesProvider
            .GetReleaseNotesAllVersionsAsync(cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        await releaseNotesEmitter.EmitAllAsync(releseNotesVersions)
            .ConfigureAwait(continueOnCapturedContext: false);

        return 0;
    }
}
