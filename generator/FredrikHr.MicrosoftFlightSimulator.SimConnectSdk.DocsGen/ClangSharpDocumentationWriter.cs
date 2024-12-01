using System.Xml;
using System.Xml.XPath;

using Microsoft.Extensions.Logging;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.DocsGen;

internal partial class ClangSharpDocumentationWriter(
    string xmlFilePath,
    XmlWriterSettings xmlWriterSettings,
    ILogger<ClangSharpDocumentationWriter>? logger = null
    ) : IDisposable
{
    private readonly XmlWriter xmlWriter = CreateXmlWriter(xmlFilePath, xmlWriterSettings);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1823: Avoid unused private fields", Justification = nameof(LoggerMessageAttribute))]
    private readonly ILogger<ClangSharpDocumentationWriter> logger =
        logger ?? Microsoft.Extensions.Logging.Abstractions
        .NullLogger<ClangSharpDocumentationWriter>.Instance;

    public void WriteSummaryElement(
        XPathNavigator topicNav
        )
    {
        xmlWriter.WriteStartElement("summary");
        try
        {
            var contentNav = topicNav.Clone();
            for (bool hasNext = contentNav.MoveToNext(XPathNodeType.Element);
                hasNext && !IsHeaderNode(contentNav);
                hasNext = contentNav.MoveToNext(XPathNodeType.Element))
            {
                if (string.IsNullOrWhiteSpace(contentNav.Value)) continue;
                WriteElement(contentNav, "topic");
            }
        }
        finally
        {
            xmlWriter.WriteEndElement();
        }

        static bool IsHeaderNode(XPathNavigator xmlNav)
        {
            string elemName = xmlNav.LocalName;
            if (elemName.Length < 1) return false;
            if (elemName[0] != 'h' && elemName[0] != 'H') return false;
            if (!char.IsDigit(elemName[1])) return false;
            return true;
        }
    }

    public void WriteCodeBlockElement(XPathNavigator codeNav, string parentName)
    {
        xmlWriter.WriteStartElement("code");
        try
        {
            bool hasMoved = false;
            for (bool hasNext = codeNav.MoveToFirstChild();
                hasNext;
                hasNext = codeNav.MoveToNext(), hasMoved = true)
            {
                switch (codeNav.NodeType)
                {
                    case XPathNodeType.Text:
                        xmlWriter.WriteString(codeNav.Value);
                        break;

                    case XPathNodeType.Element:
                        if (string.Equals("code", codeNav.LocalName, StringComparison.OrdinalIgnoreCase))
                            WriteElementContent(codeNav, "code");
                        else
                            WriteElement(codeNav, parentName);
                        break;

                    default:
                        break;
                }
            }
            if (hasMoved) codeNav.MoveToParent();
        }
        finally
        {
            xmlWriter.WriteEndElement();
        }
    }

    public void WriteSelfClosingElementWithCref(string elementName, string cref)
    {
        xmlWriter.WriteStartElement(elementName);
        xmlWriter.WriteAttributeString("cref", cref);
        xmlWriter.WriteEndElement();
    }

    public void WriteElement(XPathNavigator elemNav, string parentName)
    {
        switch (elemNav.LocalName.ToUpperInvariant())
        {
            case "P":
                WriteElementAndContent("para", elemNav);
                break;

            case "A":
                if (!string.Equals("TextPopOver", elemNav.GetAttribute("data-rhwidget", string.Empty), StringComparison.Ordinal))
                    goto case "STRONG";
                xmlWriter.WriteStartElement("em");
                xmlWriter.WriteAttributeString("title", elemNav.GetAttribute("data-popovertext", string.Empty));
                xmlWriter.WriteString(elemNav.Value);
                xmlWriter.WriteEndElement();
                break;

            case "EM":
            case "SPAN":
            case "STRONG":
                string innerContent = elemNav.Value;
                var innerTextSpanHead = innerContent.AsSpan().TrimStart();
                bool whitespaceBefore = innerTextSpanHead.Length < innerContent.Length;
                var innerTextSpan = innerTextSpanHead.TrimEnd();
                bool whitespaceAfter = innerTextSpan.Length < innerTextSpanHead.Length;
                if (whitespaceBefore)
                    xmlWriter.WriteString(" ");
                string innerText = innerTextSpan.ToString();
                if (true)
                {

                }
                else if (true)
                {

                }
                else if (true)
                {

                }
                else
                {
                    WriteElementAndContent(elemNav.LocalName, elemNav);
                }
                if (whitespaceAfter)
                    xmlWriter.WriteString(" ");
                break;

            case "CODE":
                WriteElementAndContent("c", elemNav);
                break;

            case "PRE":
                WriteCodeBlockElement(elemNav, parentName);
                break;

            default:
                LogUnexpectedChildElement(elemNav.LocalName, parentName);
                WriteElementAndContent(elemNav.LocalName, elemNav);
                break;
        }
    }

    public void WriteElementAndContent(
        string elementName,
        XPathNavigator elemNav
        )
    {
        xmlWriter.WriteStartElement(elementName);
        try
        {
            WriteElementContent(elemNav, elementName);
        }
        finally
        {
            xmlWriter.WriteEndElement();
        }
    }

    private void WriteElementContent(XPathNavigator contentNav, string parentName)
    {
        bool hasMoved = false;
        for (bool hasNext = contentNav.MoveToFirstChild();
            hasNext;
            hasNext = contentNav.MoveToNext(), hasMoved = true)
        {
            switch (contentNav.NodeType)
            {
                case XPathNodeType.Text:
                    xmlWriter.WriteString(contentNav.Value);
                    break;

                case XPathNodeType.Element:
                    WriteElement(contentNav, parentName);
                    break;

                default:
                    break;
            }
        }
        if (hasMoved) contentNav.MoveToParent();
    }

    private static XmlWriter CreateXmlWriter(string xmlFilePath, XmlWriterSettings settings)
    {
        var instSettings = settings.Clone();
        instSettings.CloseOutput = true;
        return XmlWriter.Create(xmlFilePath, instSettings);
    }

    public void Dispose()
    {
        xmlWriter.Dispose();
    }

    [LoggerMessage(LogLevel.Warning, $"Unexpected element with name <{{{nameof(childName)}}}>, child of element <{{{nameof(parentName)}}}>.")]
    private partial void LogUnexpectedChildElement(string childName, string parentName);
}
