using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

using Microsoft.Extensions.Options;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed partial class MsfsDocsReleaseNotesProvider(
    HttpClient httpClient,
    MsfsDocsTableOfContentsProvider tocProvider,
    IOptionsMonitor<XmlReaderSettings> xmlReaderSettings,
    IOptionsMonitor<XmlWriterSettings> xmlWriterSettings
    )
{
    internal const string ReleaseNotesTocItemName = "Release Notes";
    private const string XhtmlNamespace = "http://www.w3.org/1999/xhtml";

    internal async Task<Dictionary<Version, string>> GetReleaseNotesAllVersionsAsync(
        CancellationToken cancelToken = default
        )
    {
        MsfsDocsTableOfContentsTree toc = await tocProvider.GetTableOfContentsAsync()
            .ConfigureAwait(continueOnCapturedContext: false);
        MsfsDocsTableOfContentsItem? notesItem = toc.FindItemByName(ReleaseNotesTocItemName);
        if (notesItem is null) return [];

        Uri notesUri = notesItem.Uri;
        XPathDocument notesDoc = await httpClient.GetXPathDocumentAsync(
            notesUri,
            xmlReaderSettings.CurrentValue,
            cancelToken
            ).ConfigureAwait(continueOnCapturedContext: false);

        Regex notesVersionRegex = GetSdkReleaseVersionRegex();

        XPathNavigator notesDocNav = notesDoc.CreateNavigator();
        XmlNamespaceManager notesNs = new(notesDocNav.NameTable);
        notesNs.AddNamespace("xhtml", XhtmlNamespace);

        var notesTopicElement = notesDocNav.SelectSingleNode("""
            /xhtml:html/xhtml:body/xhtml:main//xhtml:div[@id="rh-topic"]
            """, notesNs);
        if (notesTopicElement is null) return [];
        var notesCurrentVersionHeaderNav = notesTopicElement.SelectSingleNode("""
            .//xhtml:h2
            """, notesNs);
        if (notesCurrentVersionHeaderNav is null) return [];
        string notesCurrentVersionHeaderText = notesCurrentVersionHeaderNav.ToString();
        Match notesVersionMatch = notesVersionRegex.Match(notesCurrentVersionHeaderText);
        if (!notesVersionMatch.Success) return [];
        if (!Version.TryParse(notesVersionMatch.Groups["version"].Value, out Version? notesCurrentVersion))
            return [];

        var notesNextVersionNav = notesTopicElement.SelectSingleNode("""
            .//xhtml:h3[text() = "Previous SDK Release Notes"]
            """, notesNs);

        List<MsfsDocsReleaseNotesVersionPointer> notesVersionPointers = [];
        notesVersionPointers.Add(new()
        {
            Heading = notesVersionMatch.Value,
            Version = notesCurrentVersion,
            ContainerNav = notesCurrentVersionHeaderNav,
            Boundary = notesNextVersionNav,
        });

        var notesPreviousVersionNavs = notesTopicElement.Select("""
            .//xhtml:a[@class="dropspot" and @data-target]
            """, notesNs);
        foreach (XPathNavigator notesVersionNav in notesPreviousVersionNavs)
        {
            string dataTargetName = notesVersionNav.GetAttribute(
                localName: "data-target",
                namespaceURI: string.Empty
                );
            if (string.IsNullOrEmpty(dataTargetName)) continue;
            XPathNavigator? dataOpenTextNav = notesVersionNav.SelectSingleNode("""
                .//xhtml:*[@data-open-text="true"]
                """, notesNs);
            if (dataOpenTextNav is null) continue;
            string notesVersionHeaderText = dataOpenTextNav.ToString();
            notesVersionMatch = notesVersionRegex.Match(notesVersionHeaderText);
            if (!notesVersionMatch.Success) continue;
            if (!Version.TryParse(notesVersionMatch.Groups["version"].Value, out Version? notesVersion)) continue;
            XPathNavigator? notesVersionContainerNav = notesTopicElement.SelectSingleNode($"""
                .//xhtml:*[@data-targetname="{dataTargetName}"]
                """, notesNs);
            if (notesVersionContainerNav is null) continue;
            notesVersionPointers.Add(new()
            {
                Heading = notesVersionMatch.Value,
                Version = notesVersion,
                ContainerNav = notesVersionContainerNav,
            });
        }

        notesVersionPointers.Sort(
            static (a, b) => -a.Version.CompareTo(b.Version)
            );

#pragma warning disable IDE0028 // Simplify collection initialization
        Dictionary<Version, string> notesVersionHtmlTexts =
            new(capacity: notesVersionPointers.Count);
#pragma warning restore IDE0028 // Simplify collection initialization
        for (int notesVersionPointerIdx = 0;
            notesVersionPointerIdx < notesVersionPointers.Count;
            notesVersionPointerIdx++)
        {
            var notesVersionPointerItem = notesVersionPointers[notesVersionPointerIdx];
            string htmlText = CreateReleaseNotesVersionDocument(
                notesUri,
                notesVersionPointers,
                notesVersionPointerIdx
                );
            notesVersionHtmlTexts[notesVersionPointerItem.Version] = htmlText;
        }
        return notesVersionHtmlTexts;
    }

    private string CreateReleaseNotesVersionDocument(
        Uri uri,
        List<MsfsDocsReleaseNotesVersionPointer> pointers,
        int pointerIdx
        )
    {
        MsfsDocsReleaseNotesVersionPointer pointer = pointers[pointerIdx];
        string htmlHeading = $"Release Notes - {pointer.Heading}";
        XmlWriterSettings htmlSettings = xmlWriterSettings.CurrentValue.Clone();
        htmlSettings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
        htmlSettings.OmitXmlDeclaration = true;
        htmlSettings.WriteEndDocumentOnClose = true;
        StringBuilder htmlBuilder = new();
        using (XmlWriter htmlWriter = XmlWriter.Create(htmlBuilder, htmlSettings))
        {
            htmlWriter.WriteDocType("html", default, default, default);
            htmlWriter.WriteStartElement("html", XhtmlNamespace);

            htmlWriter.WriteStartElement("head");

            htmlWriter.WriteElementString("title", htmlHeading);

            htmlWriter.WriteStartElement("meta");
            htmlWriter.WriteAttributeString("charset", Encoding.UTF8.WebName);
            htmlWriter.WriteEndElement(); // meta charset=utf-8

            htmlWriter.WriteStartElement("base");
            htmlWriter.WriteAttributeString("href", uri.ToString());
            htmlWriter.WriteEndElement(); // base

            htmlWriter.WriteEndElement(); // head

            htmlWriter.WriteStartElement("body");

            htmlWriter.WriteElementString("h1", "Release Notes");

            pointer.WriteTo(htmlWriter);

            bool hasPreviousVersions = false;
            for (int prevPointerIdx = pointerIdx + 1;
                prevPointerIdx < pointers.Count;
                prevPointerIdx++
                )
            {
                if (!hasPreviousVersions)
                {
                    htmlWriter.WriteElementString("h1", "Previous SDK Release Notes");
                    htmlWriter.WriteElementString("p", "Below you can find a list of the list notes for previous releases of this SDK.");
                    hasPreviousVersions = true;
                }

                pointer = pointers[prevPointerIdx];
                pointer.WriteTo(htmlWriter);
            }

            htmlWriter.WriteEndElement(); // body

            htmlWriter.WriteEndElement(); // html
        }

        return htmlBuilder.ToString();
    }

    [DebuggerDisplay($"{nameof(Version)} = {{{nameof(Version)}}}; {nameof(Heading)} = {{{nameof(Heading)}}}")]
    private class MsfsDocsReleaseNotesVersionPointer
    {
        public required string Heading { get; init; }
        public required Version Version { get; init; }
        public required XPathNavigator ContainerNav { get; init; }
        public XPathNavigator? Boundary { get; init; }

        internal void WriteTo(XmlWriter htmlWriter)
        {
            XPathNavigator xnav = ContainerNav.Clone();
            if (Boundary is XPathNavigator boundary)
            {
                htmlWriter.WriteElementString("h2", Heading);
                for (
                    bool movedXNav = xnav.MoveToNext();
                    movedXNav && !xnav.IsSamePosition(boundary);
                    movedXNav = xnav.MoveToNext()
                    )
                {
                    xnav.WriteSubtree(htmlWriter);
                }
            }
            else
            {
                htmlWriter.WriteElementString("h2", Heading);
                for (
                    bool movedXNav = xnav.MoveToFirstChild();
                    movedXNav;
                    movedXNav = xnav.MoveToNext()
                    )
                {
                    xnav.WriteSubtree(htmlWriter);
                }
            }
        }
    }

    [GeneratedRegex("""
        \bSDK release\s+(?<version>\d+\.\d+\.\d+)\b
        """, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    internal static partial Regex GetSdkReleaseVersionRegex();
}
