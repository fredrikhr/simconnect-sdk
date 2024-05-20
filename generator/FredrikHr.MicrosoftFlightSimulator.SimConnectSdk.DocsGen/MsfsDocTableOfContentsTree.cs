namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class MsfsDocTableOfContentsTree
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required Uri Uri { get; init; }
    public required Dictionary<string, MsfsDocTableOfContentsItem> Items { get; init; }
    public required Dictionary<string, MsfsDocTableOfContentsTree> Subtrees { get; init; }

    public MsfsDocTableOfContentsItem? FindItemByName(string name)
    {
        if (Items.TryGetValue(name, out var item)) return item;
        foreach (var subtree in Subtrees.Values)
        {
            item = subtree.FindItemByName(name);
            if (item is not null) return item;
        }
        return null;
    }
}

internal class MsfsDocTableOfContentsItem
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required Uri Uri { get; init; }
}
