using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} {{{nameof(FieldType)},nq}} {{{nameof(Name)},nq}}")]
public partial class PInvokeTypeFieldMember : PInvokeNamedAccessDeclaration
{
    [XmlElement("type")]
    public required PInvokeStructFieldMemberTypeRef FieldType { get; init; }

    [XmlAttribute("inherited")]
    public string? InheritedTypeName { get; init; }
}
