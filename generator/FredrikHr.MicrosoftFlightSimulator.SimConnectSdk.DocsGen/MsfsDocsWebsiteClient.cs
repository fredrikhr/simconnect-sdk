using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.DependencyInjection;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class MsfsDocsWebsiteClient(HttpClient httpClient) : IDisposable
{
    internal static readonly Uri BaseAddress = new("https://docs.flightsimulator.com/html/");

    public async Task<MsfsDocTableOfContentsTree> GetTableOfContentsAsync(
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

    private async Task<MsfsDocTableOfContentsTree>
        GetMsfsDocsTocListByKeyRecursiveAsync(
        string key,
        string name,
        string type,
        Uri uri,
        CancellationToken cancelToken = default
        )
    {
        var tocList = await GetMsfsDocsTocListByKeyAsync(key, cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        var tocItems = tocList.Where(i => string.IsNullOrEmpty(i.Key))
            .ToDictionary(
                static i => i.Name,
                static i => new MsfsDocTableOfContentsItem
                {
                    Name = i.Name,
                    Type = i.Type,
                    Uri = new(BaseAddress, i.RelativeUrl),
                }
            );
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

    public static HttpClientHandler GetHttpMessageHandler(IServiceProvider serviceProvider)
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
