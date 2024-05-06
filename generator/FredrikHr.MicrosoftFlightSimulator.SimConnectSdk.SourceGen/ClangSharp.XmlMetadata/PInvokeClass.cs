using System.Diagnostics;
using System.Xml.Serialization;

namespace ClangSharp.XmlMetadata;

[XmlType]
[DebuggerDisplay($"{{{nameof(AccessSpecifier)},nq}} class {{{nameof(Name)},nq}}")]
public partial class PInvokeClass : PInvokeType { }

