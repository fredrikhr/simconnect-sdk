using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} {{{nameof(ReturnType)},nq}} this[]")]
public partial class PInvokeTypeIndexerMember
{
    [XmlAttribute("access")]
    public string? AccessSpecifier { get; init; }

    [XmlElement("type")]
    public required PInvokeTypeRef ReturnType { get; init; }

    [XmlElement("param")]
    public PInvokeParameter[]? Parameters { get; init; }

    [XmlElement("get")]
    public PInvokeTypePropertyGetterMember? Getter { get; init; }
}
