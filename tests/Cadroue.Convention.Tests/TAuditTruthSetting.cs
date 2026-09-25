namespace Convention.Tests;

internal static class TAuditTruthSetting
{
    public const int TAuditGeneration = 11;
    public const bool TAuditTruthEnforced = true;
    public const string TAuditTruthReport = "temp/audit/Custody-{0}.md";
    public const string TAuditStateSuffix = "State";
    public const string TAuditBulletinType = "LNotice";
    public const string TAuditObserverType = "PObserver";
    public const string TAuditConfiguration = "Debug";
    public const string TAuditReferenceRoot = "src/Cadroue.UIVeneer";

    public static readonly string[] TAuditShellInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
        "src/Cadroue.UIDeportment/*.cs",
    ];

    public static readonly string[] TAuditTruthInclude =
    [
        "src/Cadroue.UIDeportment/*.cs",
    ];

    public static readonly string[] TAuditFrameworkPacks =
    [
        "Microsoft.NETCore.App",
        "Microsoft.WindowsDesktop.App",
    ];

    public static readonly string[] TAuditControlBases =
    [
        "System.Windows.FrameworkElement",
        "System.Windows.FrameworkContentElement",
    ];

    public static readonly string[] TAuditOrderVerbs =
    [
        "Insert",
        "Move",
        "RemoveAt",
        "Reverse",
        "Sort",
    ];

    public static readonly string[] TAuditFillVerbs =
    [
        "Add",
        "AddRange",
        "Clear",
        "Enqueue",
        "Insert",
        "InsertRange",
        "Move",
        "Push",
        "Remove",
        "RemoveAll",
        "RemoveAt",
        "RemoveRange",
        "Reverse",
        "Sort",
        "TryAdd",
    ];

    public const string TAuditRequestPrefix = "LWork";

    public static readonly string[] TAuditSendRoots =
    [
        "LMessengerAudioDescribe",
        "LMessengerConvertDescribe",
        "LMessengerEditDescribe",
        "LMessengerFixDescribe",
        "LMessengerFunnelDescribe",
        "LMessengerMergeDescribe",
        "LMessengerSplitDescribe",
    ];

    public static readonly string[] TAuditClockTypes =
    [
        "DispatcherTimer",
        "PeriodicTimer",
        "Stopwatch",
        "Timer",
    ];

    public static readonly string[] TAuditInputMembers =
    [
        "IsChecked",
        "Password",
        "SelectedIndex",
        "SelectedItem",
        "SelectedValue",
        "Text",
        "Value",
    ];

    public static readonly string[] TAuditTruthHandles =
    [
        "LDocket",
        "LGroupSelection",
        "LMpv",
        "LPresetSelection",
        "LRelay",
        "LScheduleContract",
        "LSegment",
        "LStation",
    ];

    public static readonly IReadOnlyDictionary<string, int> TAuditTruthCeiling = new Dictionary<string, int>
    {
        ["Argument"] = 720,
        ["Guard"] = 294,
        ["Fork"] = 45,
        ["Mirror"] = 64,
        ["Mutation"] = 171,
        ["Shape"] = 1,
        ["Treat"] = 828,
        ["Taint"] = 411,
    };

    public static readonly string[] TAuditTreatVerbs =
    [
        "Aggregate",
        "All",
        "Any",
        "Average",
        "Concat",
        "Contains",
        "Count",
        "Distinct",
        "DistinctBy",
        "Except",
        "First",
        "FirstOrDefault",
        "GroupBy",
        "GroupJoin",
        "Intersect",
        "Join",
        "Last",
        "LastOrDefault",
        "Max",
        "MaxBy",
        "Min",
        "MinBy",
        "OrderBy",
        "OrderByDescending",
        "Reverse",
        "Select",
        "SelectMany",
        "Single",
        "SingleOrDefault",
        "Skip",
        "SkipWhile",
        "Sum",
        "Take",
        "TakeWhile",
        "ThenBy",
        "ThenByDescending",
        "ToDictionary",
        "ToHashSet",
        "ToLookup",
        "Union",
        "Where",
        "Zip",
    ];
}
