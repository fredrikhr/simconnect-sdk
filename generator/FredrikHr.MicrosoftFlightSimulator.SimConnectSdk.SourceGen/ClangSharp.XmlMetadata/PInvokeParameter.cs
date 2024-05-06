using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(Type)},nq}} {{{nameof(Name)},nq}}")]
public partial class PInvokeParameter : PInvokeNamedDeclaration
{
    [XmlElement("type")]
    public required PInvokeTypeRef Type { get; init; }

    [XmlElement("init")]
    public PInvokeValueExpression? DefaultValue { get; init; }
}

