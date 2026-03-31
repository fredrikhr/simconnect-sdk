using System.CommandLine.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed class CliCommandExecution(
    MsfsDocsReleaseNotesProvider releaseNotesProvider
    ) : IHostedCommandExecution
{
    public async Task<int> InvokeAsync(CancellationToken cancelToken = default)
    {
        Dictionary<Version, string> releseNotesVersions = await releaseNotesProvider
            .GetReleaseNotesAllVersionsAsync(cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        return 0;
    }
}
