using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(Name)},nq}}")]
public partial class PInvokeTypeRef
{
    [XmlIgnore]
    protected bool IsPrimitiveSpecified { get; init; }

    [XmlAttribute("primitive")]
    protected bool IsPrimitiveValue { get; init; }

    [XmlIgnore]
    public bool? IsPrimitive
    {
        get => IsPrimitiveSpecified ? IsPrimitiveValue : null;
        init
        {
            if (value.HasValue)
            {
                IsPrimitiveSpecified = true;
                IsPrimitiveValue = value.Value;
            }
            else
            {
                IsPrimitiveSpecified = false;
                IsPrimitiveValue = default;
            }
        }
    }

    [XmlAttribute("native")]
    public string? NativeName { get; init; }

    [XmlText()]
    public required string Name { get; init; }
}
