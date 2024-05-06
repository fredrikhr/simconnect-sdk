using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[DebuggerDisplay($"{{{nameof(Name)},nq}}")]
public abstract class PInvokeNamedDeclaration
{
    [XmlAttribute("name")]
    public required string Name { get; init; }
}
