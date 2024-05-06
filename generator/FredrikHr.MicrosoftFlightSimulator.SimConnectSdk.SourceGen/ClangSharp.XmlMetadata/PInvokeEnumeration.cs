using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} enum {{{nameof(Name)},nq}}")]
public partial class PInvokeEnumeration : PInvokeNamedAccessDeclaration
{
    [XmlElement("type")]
    public required PInvokeTypeRef BaseType { get; init; }

    [XmlElement("enumerator")]
    public PInvokeEnumerationMember[]? Members { get; init; }
}
