using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;

namespace FredrikHr.MicrosoftFlightSimulator.SimConnectSdk.MSBuild;

public partial class SimConnectSdkHeaderFixup : Microsoft.Build.Utilities.Task
{
    [Required]
    [Output]
    public ITaskItem[] Lines { get; set; } = default!;

    public override bool Execute()
    {
        foreach (var taskItem in Lines ?? Array.Empty<ITaskItem>())
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

    private static readonly List<Regex> regexes = new()
    {
        new(@"^#define\s+SIMCONNECT_STRING\(.*$"),
        new(@"^#define\s+SIMCONNECT_ENUM_FLAGS\s.*$"),
        new(@"^#define\s+SIMCONNECT_REFSTRUCT\s.*$"),
    };
}
