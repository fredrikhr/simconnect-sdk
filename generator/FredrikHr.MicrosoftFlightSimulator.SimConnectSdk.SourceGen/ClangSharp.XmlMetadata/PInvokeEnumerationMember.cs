using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(Name)},nq}}")]
public partial class PInvokeEnumerationMember : PInvokeNamedAccessDeclaration
{
    [XmlElement("type")]
    public required PInvokeTypeRef Type { get; init; }

    [XmlElement("value")]
    public PInvokeValueExpression? Value { get; init; }
}
