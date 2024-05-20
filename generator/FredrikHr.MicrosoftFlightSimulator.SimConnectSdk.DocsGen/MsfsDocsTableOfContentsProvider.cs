using Microsoft.Extensions.Hosting;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class MsfsDocsTableOfContentsProvider(
    MsfsDocsWebsiteClient websiteClient,
    IHostApplicationLifetime lifetime
    ) : IDisposable
{
    private readonly Task<MsfsDocTableOfContentsTree> rootTask =
        YieldAndGetTocAsync(websiteClient, lifetime.ApplicationStopping);

    public async Task<MsfsDocTableOfContentsTree> GetTableOfContentsAsync() =>
        await rootTask.ConfigureAwait(false);

    private static async Task<MsfsDocTableOfContentsTree> YieldAndGetTocAsync(
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
