using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
public partial class PInvokeCSharpCodeExpression
{
    [XmlText]
    [XmlElement("value")]
    public string[]? Values { get; init; }
}
