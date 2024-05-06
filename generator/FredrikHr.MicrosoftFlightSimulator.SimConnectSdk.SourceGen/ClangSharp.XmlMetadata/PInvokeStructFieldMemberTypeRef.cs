using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
public partial class PInvokeStructFieldMemberTypeRef : PInvokeTypeRef
{
    [XmlAttribute("count")]
    public int Count { get; init; }

    [XmlAttribute("fixed")]
    public string? FixedTypeName { get; init; }
}
