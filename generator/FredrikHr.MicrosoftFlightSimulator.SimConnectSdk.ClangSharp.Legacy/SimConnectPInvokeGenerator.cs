using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using ClangSharp;
using ClangSharp.Interop;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.ClangSharp;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1812: Avoid uninstantiated internal classes",
    Justification = nameof(Microsoft.Extensions.DependencyInjection)
    )]
internal sealed partial class SimConnectPInvokeGenerator(
    IHostEnvironment environment,
    PInvokeGeneratorConfiguration pinvokeConfig,
    SimConnectClangSharpHeader simConnectHeader,
    IOptions<XmlReaderSettings>? xmlOptions,
    ILogger<SimConnectPInvokeGenerator>? logger = null
    )
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1823: Avoid unused private fields",
        Justification = nameof(LoggerMessageAttribute)
        )]
    private readonly ILogger<SimConnectPInvokeGenerator> logger = logger ??
        Microsoft.Extensions.Logging.Abstractions.NullLogger<SimConnectPInvokeGenerator>.Instance;
    private readonly XmlReaderSettings? xmlSettings = xmlOptions?.Value;

    public void Execute()
    {
        var output = ParseHeaderFile();
        OutputCSharpFiles(output);
    }

    private Dictionary<string, XmlDocumentStream> ParseHeaderFile()
    {
        Dictionary<string, XmlDocumentStream> outputStreams = new(StringComparer.OrdinalIgnoreCase);
        using PInvokeGenerator generator = new(pinvokeConfig, (string path) =>
        {
            XmlDocumentStream xmlStream = new(xmlSettings);
            outputStreams.Add(path, xmlStream);
            return xmlStream;
        });
        string[] clangArgs = [
            "--language=c++",                   // Treat subsequent input files as having type <language>
            "-Wno-pragma-once-outside-header"   // We are processing files which may be header files
            ];
        CXTranslationUnit_Flags translationFlags = CXTranslationUnit_None;
        translationFlags |= CXTranslationUnit_IncludeAttributedTypes;               // Include attributed types in CXType
        translationFlags |= CXTranslationUnit_VisitImplicitAttributes;              // Implicit attributes should be visited
        translationFlags |= CXTranslationUnit_DetailedPreprocessingRecord;
        LogParsingHeader(simConnectHeader.FilePath);
        var parseResult = CXTranslationUnit.TryParse(
            generator.IndexHandle,
            simConnectHeader.FilePath,
            commandLineArgs: clangArgs,
            unsavedFiles: [],
            translationFlags,
            out var parseHandle
            );
        using var translationUnit = TranslationUnit.GetOrCreate(parseHandle);
        LogProcessingHeader(simConnectHeader.FilePath);
        generator.GenerateBindings(
            translationUnit,
            simConnectHeader.FilePath,
            clangCommandLineArgs: clangArgs,
            translationFlags);
        return outputStreams;
    }

    private void OutputCSharpFiles(Dictionary<string, XmlDocumentStream> outputStreams)
    {
        var outputDocuments = outputStreams.Select(kvp =>
            KeyValuePair.Create(kvp.Key, kvp.Value.GetXmlDocument())
            ).ToList();
        outputStreams.Clear();
        foreach (var (xmlFilePath, xmlDoc) in outputDocuments)
        {
            GenerateCSharpFileFromXmlFile(xmlFilePath, xmlDoc);
        }
    }

    private void GenerateCSharpFileFromXmlFile(string xmlFilePath, XPathDocument xmlDoc)
    {
        string csharpFilePath = Path.ChangeExtension(xmlFilePath, ".cs");
        var xmlNav = xmlDoc.CreateNavigator();
        var xmlNs = xmlNav.SelectSingleNode("/bindings/namespace")!;
        string csharpNs = xmlNs.GetAttribute("name", string.Empty);
        LogBindingsNamespace(csharpNs);
        HashSet<string> requiredNamespaces = new(StringComparer.Ordinal);
        using StringWriter preDeclWriter = new();

        using StringWriter csharpTextWriter = new();
        using (IndentedTextWriter csharpIndentedWriter = new(csharpTextWriter, new(' ', 4)))
        {
            csharpIndentedWriter.WriteLine($"namespace {csharpNs};");
            foreach (XPathNavigator xmlTypeElem in xmlNs.SelectChildren(XPathNodeType.Element))
            {
                switch (xmlTypeElem.LocalName)
                {
                    case "class":
                    case "struct":
                    case "enumeration":
                    case "delegate":
                    case "interface":
                        GenerateCSharpTypeFromXmlElement(csharpIndentedWriter, xmlTypeElem, requiredNamespaces);
                        break;

                    default:
                        LogErrorUnexpectedXmlElement(xmlTypeElem.LocalName);
                        break;
                }
            }
        }

        List<string> specialNamespaces = ["System", "Microsoft", "ClangSharp"];
        CSharpNamespaceComparer nsCmp = new(specialNamespaces);
        var requiredNamespaceGroups = requiredNamespaces
            .GroupBy(TopLevelNamespace)
            .OrderBy(static g => g.Key, nsCmp);

        foreach (var nsGroup in requiredNamespaceGroups)
        {
            foreach (var requiredNs in nsGroup.Order(nsCmp))
                preDeclWriter.WriteLine($"using {requiredNs};");
            preDeclWriter.WriteLine();
        }

        using StreamWriter csharpFileWriter = new(File.Open(csharpFilePath, FileMode.Create, FileAccess.Write, FileShare.None), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        csharpFileWriter.Write(preDeclWriter);
        csharpFileWriter.Write(csharpTextWriter.GetStringBuilder());

        static string TopLevelNamespace(string ns)
        {
            ns ??= string.Empty;
            int dotIdx = ns.IndexOf('.', StringComparison.Ordinal);
            return ns[(dotIdx + 1)..];
        }
    }

    private void GenerateCSharpTypeFromXmlElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator xmlTypeElem,
        HashSet<string> requiredNamespaces
        )
    {
        csharpWriter.WriteLine();

        var (name, access) = GetNameAndAccessFromXmlElement(xmlTypeElem);
        LogTypeXmlElement(xmlTypeElem.LocalName, name, access);
        switch (xmlTypeElem.LocalName)
        {
            case "class":
                GenerateCSharpClassFromXmlElement(csharpWriter, xmlTypeElem, name, access, requiredNamespaces);
                break;

            case "struct":
                GenerateCSharpStructFromXmlElement(csharpWriter, xmlTypeElem, name, access, requiredNamespaces);
                break;

            case "enumeration":
                GenerateCSharpEnumFromXmlElement(csharpWriter, xmlTypeElem, name, access);
                break;

            default:
                throw new InvalidOperationException();
        }
    }

    private void GenerateCSharpClassFromXmlElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator classElem,
        string name,
        string access,
        HashSet<string> requiredNamespaces
        )
    {
        bool isStatic = classElem.SelectSingleNode("@static")?.ValueAsBoolean ?? false;
        bool isUnsafe = classElem.SelectSingleNode("@unsafe")?.ValueAsBoolean ?? false;

        csharpWriter.Write($"{access}");
        if (isUnsafe) csharpWriter.Write(" unsafe");
        if (isStatic) csharpWriter.Write(" static");
        csharpWriter.WriteLine($" partial class {name}");

        csharpWriter.WriteLine("{");
        csharpWriter.Indent++;



        csharpWriter.Indent--;
        csharpWriter.WriteLine("}");
    }

    private void GenerateCSharpStructFromXmlElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator structElem,
        string name,
        string access,
        HashSet<string> requiredNamespaces
        )
    {
        string? nativeTypeName = structElem.GetAttribute("native", string.Empty);
        string? parentTypeName = structElem.GetAttribute("parent", string.Empty);
        Guid? guid = (Guid?)structElem.SelectSingleNode("@uuid")
            ?.ValueAs(typeof(Guid));
        _ = bool.TryParse(structElem.GetAttribute("vtbl", string.Empty), out bool hasVtbl);
        _ = bool.TryParse(structElem.GetAttribute("unsafe", string.Empty), out bool isUnsafe);
        _ = Enum.TryParse(structElem.GetAttribute("layout", string.Empty), out LayoutKind layoutKind);
        int? pack = structElem.SelectSingleNode("@pack")?.ValueAsInt;
        isUnsafe = structElem.SelectSingleNode("""field/type[@fixed!=""]""") is not null;

        if (guid.HasValue)
        {
            requiredNamespaces.Add(typeof(GuidAttribute).Namespace!);
            csharpWriter.WriteLine($"[Guid({guid.Value})]");
        }

        if (!string.IsNullOrEmpty(nativeTypeName))
        {
            requiredNamespaces.Add($"{nameof(ClangSharp)}.{nameof(global::ClangSharp.Interop)}");
            csharpWriter.WriteLine($"""[NativeTypeName("{nativeTypeName}")]""");
        }

        requiredNamespaces.Add(typeof(StructLayoutAttribute).Namespace!);
        requiredNamespaces.Add(typeof(LayoutKind).Namespace!);
        csharpWriter.Write(FormattableString.Invariant($"[StructLayout(LayoutKind.{layoutKind}"));
        if (pack.HasValue) csharpWriter.Write(FormattableString.Invariant($", Pack = {pack.Value}"));
        csharpWriter.WriteLine(")]");

        csharpWriter.Write($"{access}");
        if (isUnsafe) csharpWriter.Write(" unsafe");
        csharpWriter.WriteLine($" readonly partial struct {name}");

        csharpWriter.WriteLine("{");
        csharpWriter.Indent++;

        XPathNavigator xnav = structElem.Clone();
        for (
            bool xnavContinue = xnav.MoveToFirstChild();
            xnavContinue;
            xnavContinue = xnav.MoveToNext()
            )
        {
            switch ((xnav.NodeType, xnav.LocalName))
            {
                case (XPathNodeType.Element, "field"):
                    GenerateCSharpTypeFieldMemberFromXmlElement(csharpWriter, xnav, requiredNamespaces);
                    break;
                case (XPathNodeType.Element, "constant"):
                    GenerateCSharpTypeConstantMemberFromXmlElement(csharpWriter, xnav);
                    break;
                case (XPathNodeType.Element, "struct"):
                    break;
                default:
                    throw new XmlException($"Unexpected element (NodeType: {xnav.NodeType}, LocalName: {xnav.LocalName}) as child of <struct> Element");
            }
        }

        csharpWriter.Indent--;
        csharpWriter.WriteLine("}");
    }

    private void GenerateCSharpTypeFieldMemberFromXmlElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator fieldElem,
        HashSet<string> requiredNamespaces
        )
    {
        var (name, access) = GetNameAndAccessFromXmlElement(fieldElem);
        string fieldType = "int";
        string? nativeTypeName = null;
        string? fixedName = null;
        int? fixedCount = null;
        bool fixedIsPrimitive = false;
        if (fieldElem.SelectSingleNode("type") is XPathNavigator typeElem)
        {
            fieldType = typeElem.Value;
            nativeTypeName = typeElem.GetAttribute("native", string.Empty);
            fixedCount = typeElem.SelectSingleNode("@count")?.ValueAsInt;
            fixedName = typeElem.SelectSingleNode("@fixed")?.Value;
            fixedIsPrimitive = fixedName is not null
                && fixedBufferTypes.Contains(fieldType);
        }

        if (!string.IsNullOrEmpty(nativeTypeName))
        {
            requiredNamespaces.Add($"{nameof(ClangSharp)}.{nameof(global::ClangSharp.Interop)}");
            csharpWriter.WriteLine($"""[NativeTypeName("{nativeTypeName}")]""");
        }

        var (fieldName, propertyName) = GetMemberNames(name);
        if (fixedIsPrimitive)
        {
            string fixedBufferLength = fixedCount.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            csharpWriter.WriteLine($"{access} readonly fixed {fieldType} {fieldName}[{fixedBufferLength}];");
            csharpWriter.WriteLine($"private const int {fieldName}BufferLength = {fixedBufferLength};");
        }
        else if (fixedCount.HasValue)
        {
            throw new NotImplementedException();
        }
        else
        {
            csharpWriter.WriteLine($"private readonly {fieldType} {fieldName};");
            csharpWriter.WriteLine($"{access} {fieldType} {propertyName} => {fieldName};");
        }

        static (string fieldName, string propertyName) GetMemberNames(string name)
        {
            var nameSpan = name.AsSpan();
            int upperIdx = nameSpan.IndexOfAnyInRange('A', 'Z');
            return upperIdx switch
            {
                >0 => (name, EscapeMemberName(nameSpan[upperIdx..].ToString())),
                0 => (EscapeMemberName(string.Create(name.Length, name, static (s, n) =>
                {
                    n.CopyTo(s);
                    s[0] = char.ToLowerInvariant(s[0]);
                })), name),
                <0 => (name, EscapeMemberName(string.Create(name.Length, name, (s, n) =>
                {
                    n.CopyTo(s);
                    s[0] = char.ToUpperInvariant(s[0]);
                }))),
            };
        }

        static string EscapeMemberName(string name) => name switch
        {
            "base" => "@base",
            _ => name,
        };
    }

    private static void GenerateCSharpTypeConstantMemberFromXmlElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator constantElem
        )
    {
        var (name, access) = GetNameAndAccessFromXmlElement(constantElem);
        string fieldType = "int";
        bool isPrimitive = false;
        if (constantElem.SelectSingleNode("type") is XPathNavigator typeElem)
        {
            fieldType = typeElem.Value;
            _ = bool.TryParse(typeElem.GetAttribute("primitive", string.Empty), out isPrimitive);
        }

        csharpWriter.Write($"{access} {(isPrimitive ? "const" : "static readonly")} {fieldType} {name} = ");
        WriteCSharpCodeFromValueElement(csharpWriter, constantElem.SelectSingleNode("value")!, fieldType);
        csharpWriter.WriteLine(";");
    }

    private void GenerateCSharpEnumFromXmlElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator enumElem,
        string name,
        string access)
    {
        string? baseType = enumElem.SelectSingleNode("type")?.Value;
        csharpWriter.Write($"{access} enum {name}");
        if (!string.IsNullOrEmpty(baseType)) csharpWriter.Write($" : {baseType}");
        else baseType = "int";
        csharpWriter.WriteLine();

        csharpWriter.WriteLine("{");
        csharpWriter.Indent++;

        foreach (XPathNavigator fieldElem in enumElem.SelectChildren("enumerator", string.Empty))
        {
            var (fieldName, _) = GetNameAndAccessFromXmlElement(fieldElem);
            csharpWriter.Write(fieldName);
            string fieldExprType = fieldElem.SelectSingleNode("type")?.Value ?? baseType;
            XPathNavigator? valueElem = fieldElem.SelectSingleNode("value");
            if (valueElem is not null)
            {
                csharpWriter.Write(" = ");
                bool castToBaseType = !string.Equals(baseType, fieldExprType, StringComparison.Ordinal);
                if (castToBaseType)
                {
                    csharpWriter.Write($"unchecked(({baseType})(");
                }

                WriteCSharpCodeFromValueElement(csharpWriter, valueElem, fieldExprType);

                if (castToBaseType)
                {
                    csharpWriter.Write("))");
                }
            }
            csharpWriter.WriteLine(",");
        }

        csharpWriter.Indent--;
        csharpWriter.WriteLine("}");
    }

    private static void WriteCSharpCodeFromValueElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator valueElem,
        string valueType
        )
    {
        XPathNavigator innerElem = valueElem.Clone();
        if (innerElem.MoveToChild("unchecked", string.Empty))
            WriteCSharpCodeFromUncheckedElement(csharpWriter, innerElem, valueType);
        else if (innerElem.MoveToChild("code", string.Empty))
            WriteCSharpCodeFromCodeElement(csharpWriter, innerElem);
        else
            csharpWriter.Write("default");
    }

    private static void WriteCSharpCodeFromUncheckedElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator uncheckedElem,
        string valueType
        )
    {
        XPathNavigator? valueElem = uncheckedElem.SelectSingleNode("value")
            ?? throw new XmlException("Missing expected child element <value> of <unchecked> Element");
        csharpWriter.Write($"unchecked(({valueType})");
        WriteCSharpCodeFromValueElement(csharpWriter, valueElem, valueType);
        csharpWriter.Write(")");
    }

    private static void WriteCSharpCodeFromCodeElement(
        IndentedTextWriter csharpWriter,
        XPathNavigator codeElem
        )
    {
        XPathNavigator xnav = codeElem.Clone();
        for (bool movedToElem = xnav.MoveToFirstChild();
            movedToElem;
            movedToElem = xnav.MoveToNext())
        {
            switch ((xnav.NodeType, xnav.LocalName))
            {
                case (XPathNodeType.Text, _):
                case (XPathNodeType.Element, "value"):
                    csharpWriter.Write(xnav.Value);
                    break;

                default:
                    throw new XmlException($"Unexpected element (NodeType: {xnav.NodeType}, LocalName: {xnav.LocalName}) as child of <code> Element");
            }
        }
    }

    private static readonly HashSet<string> fixedBufferTypes = [
        "bool", "byte", "char", "short", "int", "long",
        "sbyte", "ushort", "ulong", "float", "double"
        ];

    private static (string name, string access) GetNameAndAccessFromXmlElement(XPathNavigator xmlElem)
    {
        return (
            xmlElem.GetAttribute("name", string.Empty),
            xmlElem.GetAttribute("access", string.Empty)
            );
    }

    [LoggerMessage(LogLevel.Debug, $"Parsing C++ header file: {{{nameof(filePath)}}}")]
    private partial void LogParsingHeader(string filePath);

    [LoggerMessage(LogLevel.Information, $"Processing header file: {{{nameof(filePath)}}}")]
    private partial void LogProcessingHeader(string filePath);

    [LoggerMessage(LogLevel.Error, $"Unexpected XML Element: {{{nameof(elementName)}}}")]
    private partial void LogErrorUnexpectedXmlElement(string elementName);

    [LoggerMessage(LogLevel.Trace, $"Bindings namespace: {{{nameof(@namespace)}}}")]
    private partial void LogBindingsNamespace(string @namespace);

    [LoggerMessage(LogLevel.Information, $"Type XML Element: {{{nameof(accessModifier)}}} {{{nameof(elementName)}}} {{{nameof(name)}}}")]
    private partial void LogTypeXmlElement(string elementName, string name, string accessModifier);
}
