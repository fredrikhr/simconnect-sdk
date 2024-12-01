using System.Xml.XPath;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal class ClangSharpXmlBindingsReader
{
    public static HashSet<string> GetEnumMemberNames(
        XPathNavigator enumTypeNav
        )
    {
        return new(enumTypeNav.SelectChildren("enumerator", string.Empty)
            .Cast<XPathNavigator>()
            .Select(nav => nav.GetAttribute("name", string.Empty))
            );
    }

    public static HashSet<string> GetStructFieldNames(
        XPathNavigator structTypeNav
        )
    {
        return new(structTypeNav
            .SelectChildren("field", string.Empty).Cast<XPathNavigator>()
            .Concat(structTypeNav
            .SelectChildren("constant", string.Empty).Cast<XPathNavigator>())
            .Select(nav => nav.GetAttribute("name", string.Empty))
            );
    }
}
