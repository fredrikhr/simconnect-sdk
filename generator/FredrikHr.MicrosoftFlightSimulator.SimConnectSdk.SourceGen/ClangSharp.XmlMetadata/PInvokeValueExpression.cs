using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
public partial class PInvokeValueExpression
{
    [XmlElement("code")]
    public PInvokeCSharpCodeExpression? CSharpCode { get; init; }

    [XmlElement("unchecked")]
    public PInvokeUncheckedExpression? UncheckedExpression { get; init; }
}
