using System.Diagnostics;
using System.Xml.Serialization;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1819: Properties should not return arrays",
    Scope = "namespaceAndDescendants",
    Target = "~N:ClangSharp.XmlMetadata",
    Justification = nameof(System.Xml.Serialization)
    )]

namespace ClangSharp.XmlMetadata;

[XmlType]
[XmlRoot("bindings")]
[DebuggerDisplay($"Bindings = {{{nameof(Namespace)},nq}}")]
public partial class PInvokeBindings
{
    [XmlElement("namespace")]
    public required PInvokeNamespace Namespace { get; init; }
}
