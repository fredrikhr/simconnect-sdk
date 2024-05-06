using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
public partial class PInvokeUncheckedExpression
{
    [XmlElement("value")]
    public required PInvokeValueExpression Expression { get; init; }
}

