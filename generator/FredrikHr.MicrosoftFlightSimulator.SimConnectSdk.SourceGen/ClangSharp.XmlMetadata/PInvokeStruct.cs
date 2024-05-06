using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} struct {{{nameof(Name)},nq}}")]
public partial class PInvokeStruct : PInvokeType
{
    [XmlAttribute("layout")]
    public string? LayoutKind { get; init; }

    [XmlAttribute("pack")]
    protected int PackValue { get; init; }

    [XmlIgnore]
    protected bool PackSpecified { get; set; }

    [XmlIgnore]
    public int? Pack
    {
        get => PackSpecified ? PackValue : null;
        init => (PackSpecified, PackValue) =
            (value.HasValue, value.GetValueOrDefault());
    }
}
