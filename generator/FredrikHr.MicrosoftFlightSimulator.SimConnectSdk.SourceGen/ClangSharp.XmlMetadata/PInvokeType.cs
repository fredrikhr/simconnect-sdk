using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} type {{{nameof(Name)},nq}}")]
public abstract partial class PInvokeType : PInvokeNamedAccessDeclaration
{
    [XmlElement("attribute")]
    public string[]? Attributes { get; init; }

    [XmlElement("field")]
    public PInvokeTypeFieldMember[]? Fields { get; init; }

    [XmlElement("constant")]
    public PInvokeConstant[]? Constants { get; set; }

    [XmlElement("class", typeof(PInvokeClass))]
    [XmlElement("struct", typeof(PInvokeStruct))]
    public PInvokeType[]? NestedTypes { get; init; }

    [XmlElement("function")]
    public PInvokeTypeMethodMember[]? Methods { get; init; }

    [XmlElement("indexer")]
    public PInvokeTypeIndexerMember[]? Indexers { get; init; }
}
