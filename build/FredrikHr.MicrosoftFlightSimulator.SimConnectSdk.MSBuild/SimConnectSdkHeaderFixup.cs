using System.Text.RegularExpressions;

using Microsoft.Build.Framework;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.MSBuild;

public partial class SimConnectSdkHeaderFixup : Microsoft.Build.Utilities.Task
{
    [Required]
    [Output]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1819: Properties should not return arrays",
        Justification = nameof(ITask)
        )]
    public ITaskItem[] Lines { get; set; } = default!;

    public override bool Execute()
    {
        foreach (var taskItem in Lines ?? [])
        {
            string inputText = taskItem.ItemSpec;
            string replacedText = inputText;
            foreach (var regex in regexes)
            {
                replacedText = regex.Replace(replacedText, @"/*$0*/");
            }

            taskItem.ItemSpec = replacedText;
        }

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "SYSLIB1045: Convert to 'GeneratedRegexAttribute'.",
        Justification = ".NET Framework Compatibility"
        )]
    private static readonly List<Regex> regexes =
    [
        new(@"^#define\s+SIMCONNECT_STRING\(.*$"),
        new(@"^#define\s+SIMCONNECT_ENUM_FLAGS\s.*$"),
        new(@"^#define\s+SIMCONNECT_REFSTRUCT\s.*$"),
    ];
}
