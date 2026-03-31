using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed class MsfsDocsWebsiteClient(HttpClient httpClient) : IDisposable
{
    internal static readonly Uri BaseAddress = new("https://docs.flightsimulator.com/html/");

    internal async Task<MsfsDocsTableOfContentsTree> GetTableOfContentsAsync(
        CancellationToken cancelToken = default)
    {
        var rootTree = await GetMsfsDocsTocListByKeyRecursiveAsync(
            "toc",
            "",
            "root",
            new(BaseAddress, "index.htm"),
            cancelToken
            ).ConfigureAwait(continueOnCapturedContext: false);
        return rootTree;
    }

    private async Task<MsfsDocsTableOfContentsTree>
        GetMsfsDocsTocListByKeyRecursiveAsync(
        string key,
        string name,
        string type,
        Uri uri,
        CancellationToken cancelToken = default
        )
    {
        List<MsfsDocsTableOfContentsRawItem> tocList =
            await GetMsfsDocsTocListByKeyAsync(key, cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        Dictionary<string, MsfsDocsTableOfContentsItem> tocItems =
            new(StringComparer.InvariantCultureIgnoreCase);
        foreach (
            MsfsDocsTableOfContentsRawItem? keyLessRawItem in
            tocList.Where(i => string.IsNullOrEmpty(i.Key))
            )
        {
            tocItems[keyLessRawItem.Name] = new()
            {
                Name = keyLessRawItem.Name,
                Type = keyLessRawItem.Type,
                Uri = new(BaseAddress, keyLessRawItem.RelativeUrl),
            };
        }
        var tocKeyTasks = tocList.Where(i => !string.IsNullOrEmpty(i.Key))
            .Select(i => GetMsfsDocsTocListByKeyRecursiveAsync(
                i.Key!,
                i.Name,
                i.Type,
                new Uri(BaseAddress, i.RelativeUrl),
                cancelToken
                )
            );
        var tocKeyList = await Task.WhenAll(tocKeyTasks)
            .ConfigureAwait(continueOnCapturedContext: false);
        var tocKeys = (tocKeyList ?? [])
            .ToDictionary(static i => i.Name);
        return new()
        {
            Name = name,
            Type = type,
            Uri = uri,
            Items = tocItems,
            Subtrees = tocKeys,
        };
    }

    private async Task<List<MsfsDocsTableOfContentsRawItem>>
        GetMsfsDocsTocListByKeyAsync(
        string key,
        CancellationToken cancelToken = default
        )
    {
        Uri tocUri = new(BaseAddress, $"whxdata/{key}.new.js");
        var tocBytes = await httpClient.GetByteArrayAsync(
            tocUri,
            cancelToken
            ).ConfigureAwait(continueOnCapturedContext: false);
        var tocJson = ReadFirstInnerDocument(tocBytes, "["u8);
        var tocList = tocJson.Deserialize<List<MsfsDocsTableOfContentsRawItem>>();
        return tocList ?? [];
    }

    [System.Diagnostics.DebuggerDisplay($"{nameof(Key)} = {{{nameof(Key)}}}; {nameof(Name)} = {{{nameof(Name)}}}; {nameof(Type)} = {{{nameof(Type)}}}; Url = {{{nameof(RelativeUrl)}}}")]
    private class MsfsDocsTableOfContentsRawItem
    {
        [JsonPropertyName("name")]
        public required string Name { get; init; }
        [JsonPropertyName("type")]
        public required string Type { get; init; }
        [JsonPropertyName("url")]
        public string? RelativeUrl { get; init; }
        [JsonPropertyName("key")]
        public string? Key { get; init; }
    }

    private static JsonDocument ReadFirstInnerDocument(
        ReadOnlySpan<byte> utfJsonData,
        ReadOnlySpan<byte> utfJsonStart
        )
    {
        for (
            int offset = utfJsonData.IndexOf(utfJsonStart);
            offset >= 0;
            utfJsonData = utfJsonData[(offset + 1)..],
            offset = utfJsonData.IndexOf(utfJsonStart)
            )
        {
            utfJsonData = utfJsonData[offset..];
            Utf8JsonReader jsonReader = new(utfJsonData, new()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
            try
            {
                JsonDocument jsonDoc = JsonDocument.ParseValue(ref jsonReader);
                return jsonDoc;
            }
            catch (JsonException)
            {
                continue;
            }
        }

        throw new JsonException();
    }

    internal static HttpClientHandler GetHttpMessageHandler(IServiceProvider serviceProvider)
    {
        var cookies = serviceProvider.GetRequiredService<CookieContainer>();
        return new()
        {
            CookieContainer = cookies,
            UseCookies = true,
        };
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
