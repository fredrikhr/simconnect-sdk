using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} {{{nameof(ReturnType)},nq}} {{{nameof(Name)},nq}}()")]
public partial class PInvokeTypeMethodMember : PInvokeNamedAccessDeclaration
{
    [XmlElement("type")]
    public required PInvokeTypeRef ReturnType { get; init; }

    [XmlElement("param")]
    public PInvokeParameter[]? Parameters { get; init; }

    [XmlAttribute("lib")]
    public string? LibraryName { get; init; }

    [XmlAttribute("convention")]
    public string? CallingConvention { get; init; }

    [XmlAttribute("unsafe")]
    public bool IsUnsafe { get; init; }

    [XmlElement("code")]
    public PInvokeCSharpCodeExpression? CSharpCode { get; init; }
}

