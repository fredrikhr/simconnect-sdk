using System.Diagnostics.CodeAnalysis;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.ClangSharp;

internal class CSharpNamespaceComparer(
    IList<string> firstNamespaces
    ) : IComparer<string>, IEqualityComparer<string>
{
    private static readonly StringComparer strCmp = StringComparer.Ordinal;

    public int Compare(string? x, string? y)
    {
        if (x is null) return y is null ? 0 : 1;
        if (y is null) return -1;

        return CompareNamespace(x, y, first: true);
    }

    private int CompareNamespace(ReadOnlySpan<char> x, ReadOnlySpan<char> y, bool first = false)
    {
        if (first)
        {
            int xFirstNsIdx = IndexOfFirstNamespace(x);
            int yFirstNsIdx = IndexOfFirstNamespace(y);
            if (xFirstNsIdx >= 0)
                return yFirstNsIdx >= 0 ? yFirstNsIdx - xFirstNsIdx : -1;
            else if (yFirstNsIdx >= 0) return 1;
        }

        int xNsIdx = x.IndexOf('.');
        int yNsIdx = y.IndexOf('.');

        var xNs = xNsIdx < 0 ? x : x[..xNsIdx];
        var yNs = yNsIdx < 0 ? y : y[..yNsIdx];

        return xNs.CompareTo(yNs, StringComparison.Ordinal) switch
        {
            < 0 => -1,
            > 0 => 1,
            0 => xNsIdx switch
            {
                < 0 => yNsIdx switch
                {
                    < 0 => 0,
                    _ => -1,
                },
                _ => yNsIdx switch
                {
                    < 0 => 1,
                    _ => CompareNamespace(x[(xNsIdx + 1)..], y[(yNsIdx + 1)..]),
                },
            }
        };
    }

    private int IndexOfFirstNamespace(ReadOnlySpan<char> ns)
    {
        for (int i = 0; i < firstNamespaces.Count; i++)
        {
            var firstNs = firstNamespaces[i].AsSpan();
            if (firstNs.SequenceEqual(ns))
                return i;
        }
        return -1;
    }

    public bool Equals(string? x, string? y) => strCmp.Equals(x, y);

    public int GetHashCode([DisallowNull] string obj) => strCmp.GetHashCode(obj);
}
