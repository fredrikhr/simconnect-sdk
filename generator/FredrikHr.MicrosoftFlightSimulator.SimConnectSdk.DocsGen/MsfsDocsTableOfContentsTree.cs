namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal sealed class MsfsDocsTableOfContentsTree
{
    internal required string Name { get; init; }
    internal required string Type { get; init; }
    internal required Uri Uri { get; init; }
    internal required Dictionary<string, MsfsDocsTableOfContentsItem> Items { get; init; }
    internal required Dictionary<string, MsfsDocsTableOfContentsTree> Subtrees { get; init; }

    internal MsfsDocsTableOfContentsItem? FindItemByName(string name)
    {
        if (Items.TryGetValue(name, out MsfsDocsTableOfContentsItem? item))
            return item;
        foreach (var subtree in Subtrees.Values)
        {
            item = subtree.FindItemByName(name);
            if (item is not null) return item;
        }
        return null;
    }
}
