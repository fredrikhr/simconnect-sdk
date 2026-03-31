using Microsoft.Extensions.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed class MsfsDocsTableOfContentsProvider(
    MsfsDocsWebsiteClient websiteClient,
    IHostApplicationLifetime lifetime
    ) : IDisposable
{
    private readonly Task<MsfsDocsTableOfContentsTree> rootTask =
        YieldAndGetTocAsync(websiteClient, lifetime.ApplicationStopping);

    internal async Task<MsfsDocsTableOfContentsTree> GetTableOfContentsAsync() =>
        await rootTask.ConfigureAwait(false);

    private static async Task<MsfsDocsTableOfContentsTree> YieldAndGetTocAsync(
        MsfsDocsWebsiteClient websiteClient,
        CancellationToken cancelToken = default
        )
    {
        await Task.Yield();
        return await websiteClient.GetTableOfContentsAsync(cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    public void Dispose()
    {
        websiteClient.Dispose();
    }
}
