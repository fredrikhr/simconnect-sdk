using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} {{{nameof(Name)},nq}}")]
public abstract class PInvokeNamedAccessDeclaration : PInvokeNamedDeclaration
{
    [XmlAttribute("access")]
    public string? AccessSpecifier { get; init; }

    [XmlAttribute("static")]
    public bool IsStatic { get; init; }

    [XmlAttribute("native")]
    public string? NativeName { get; init; }
}
