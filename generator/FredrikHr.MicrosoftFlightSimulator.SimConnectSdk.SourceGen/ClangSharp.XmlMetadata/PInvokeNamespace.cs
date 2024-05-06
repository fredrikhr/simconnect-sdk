using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(Name)},nq}}")]
public partial class PInvokeNamespace : PInvokeNamedDeclaration
{
    [XmlElement("class")]
    public PInvokeClass[]? Classes { get; init; }

    [XmlElement("enumeration")]
    public PInvokeEnumeration[]? Enums { get; init; }

    [XmlElement("struct")]
    public PInvokeStruct[]? Structs { get; init; }
}
