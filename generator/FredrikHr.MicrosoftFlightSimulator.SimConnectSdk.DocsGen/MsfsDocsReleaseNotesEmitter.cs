using System.Text;
using System.Text.Json;
using System.Threading;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen.CommandLine;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class MsfsDocsReleaseNotesEmitter
{
    private static readonly UTF8Encoding HtmlEncoding =
        new(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    private readonly string _releasesDirectoryPath;
    private readonly string _releasesJsonFilePath;
    private readonly Task<HashSet<string>> _versionStrings;
    private readonly CancellationToken _cancelToken;
    private readonly Lock _lock = new();

    public MsfsDocsReleaseNotesEmitter(
        IHostEnvironment hostingEnvironment,
        IHostApplicationLifetime lifetime
        )
    {
        if (lifetime is not { ApplicationStopping: CancellationToken cancelToken })
            cancelToken = CancellationToken.None;
        _cancelToken = cancelToken;
        string? contentRootPath = hostingEnvironment?.ContentRootPath;
        if (!string.IsNullOrEmpty(contentRootPath))
        {
            try { contentRootPath = Path.GetFullPath(contentRootPath); }
            catch (ArgumentException) { contentRootPath = default!;}
        }
        contentRootPath ??= Environment.CurrentDirectory;
        _releasesDirectoryPath = Path.Combine(contentRootPath, "releases");
        _releasesJsonFilePath = Path.Combine(contentRootPath, "releases.json");
        _versionStrings = InitializeVersionStringsAsync(
            _releasesJsonFilePath,
            cancelToken
            );
    }

    private static async Task<HashSet<string>> InitializeVersionStringsAsync(
        string releasesJsonFilePath,
        CancellationToken cancelToken
        )
    {
        IEqualityComparer<string> cmp = StringComparer.OrdinalIgnoreCase;
        using FileStream? releasesJsonFileStream = OpenFileStream(releasesJsonFilePath);
        if (releasesJsonFileStream is null)
        { return new HashSet<string>(cmp); }

        List<string>? versionsJsonData = await JsonSerializer
            .DeserializeAsync<List<string>>(releasesJsonFileStream, options: default, cancelToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        versionsJsonData ??= [];
        for (int i = 0; i < versionsJsonData.Count; i++)
        {
            string versionEntry = versionsJsonData[i];
            if (!Version.TryParse(versionEntry, out _))
            {
                versionsJsonData.RemoveAt(i);
                i--;
            }
        }

        return new HashSet<string>(versionsJsonData, cmp);

        static FileStream? OpenFileStream(string path)
        {
            try { return File.OpenRead(path); }
            catch (IOException) { return null; }
        }
    }

    private async Task EmitHtmlTextOnlyAsync(Version version, string htmlText)
    {
        string versionString = version.ToString(3);
        string releaseNotesDirectoryPath = Path.Combine(
            _releasesDirectoryPath,
            versionString
            );
        _ = Directory.CreateDirectory(releaseNotesDirectoryPath);
        string releaseNotesFilePath = Path.Combine(
            releaseNotesDirectoryPath,
            "Release Notes.html"
            );
        await File.WriteAllTextAsync(
            releaseNotesFilePath,
            htmlText,
            HtmlEncoding,
            _cancelToken
            ).ConfigureAwait(continueOnCapturedContext: false);
    }

    internal async Task EmitAsync(Version version, string htmlText)
    {
        Task emitHtmlTask = EmitHtmlTextOnlyAsync(version, htmlText);
        HashSet<string> versionsHashSet = await _versionStrings
            .ConfigureAwait(continueOnCapturedContext: false);
        lock (_lock) { versionsHashSet.Add(version.ToString(fieldCount: 3)); }
        await Task.WhenAll(emitHtmlTask, SaveVersionsToJsonFileAsync())
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    internal async Task EmitAllAsync(
        Dictionary<Version, string> versionHtmlTextPairs
        )
    {
        IEnumerable<Task> emitHtmlTasks = versionHtmlTextPairs
            .Select(entry => EmitHtmlTextOnlyAsync(entry.Key, entry.Value));
        await Task.WhenAll(emitHtmlTasks)
            .ConfigureAwait(continueOnCapturedContext: false);
        IEnumerable<string> versionsToAdd = versionHtmlTextPairs.Keys
            .Select(v => v.ToString(fieldCount: 3));
        HashSet<string> versionsHashSet = await _versionStrings
            .ConfigureAwait(continueOnCapturedContext: false);
        lock (_lock) { versionsHashSet.UnionWith(versionsToAdd); }
        await SaveVersionsToJsonFileAsync()
            .ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task SaveVersionsToJsonFileAsync()
    {
        HashSet<string> versionsHashSet = await _versionStrings
            .ConfigureAwait(continueOnCapturedContext: false);
        List<Version> versionsList;
        lock (_lock)
        {
            versionsList = [
                .. versionsHashSet
                .Select(Version.Parse)
                .OrderDescending()
                ];
        }
        List<string> versionsJsonData = [
            ..versionsList.Select(v => v.ToString(fieldCount: 3))
            ];
        _ = Directory.CreateDirectory(_releasesDirectoryPath);
        using FileStream jsonFileStream = File.Open(
            _releasesJsonFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read
            );
        await JsonSerializer.SerializeAsync(
            jsonFileStream,
            versionsJsonData,
            JsonOptions
            ).ConfigureAwait(continueOnCapturedContext: false);
    }
}
