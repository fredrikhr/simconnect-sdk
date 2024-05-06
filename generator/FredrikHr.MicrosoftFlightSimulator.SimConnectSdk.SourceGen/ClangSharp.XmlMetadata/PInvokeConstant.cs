using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} const {{{nameof(Type)},nq}} {{{nameof(Name)},nq}}")]
public partial class PInvokeConstant : PInvokeNamedAccessDeclaration
{
    [XmlElement("type")]
    public required PInvokeTypeRef Type { get; init; }

    [XmlElement("value")]
    public required PInvokeValueExpression Value { get; init; }
}

