using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
public partial class PInvokeTypePropertyGetterMember
{
    [XmlElement("code")]
    public PInvokeCSharpCodeExpression? CSharpCode { get; init; }
}
