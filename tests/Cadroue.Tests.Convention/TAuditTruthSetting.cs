namespace Convention.Tests;

internal static class TAuditTruthSetting
{
    public const int TAuditGeneration = 25;
    public const bool TAuditTruthEnforced = true;
    public const string TAuditTruthReport = "temp/audit/Custody-{0}.md";
    public const string TAuditStateSuffix = "State";
    public const string TAuditBulletinType = "LBulletin";
    public const string TAuditConfiguration = "Debug";
    public const string TAuditReferenceRoot = "src/Cadroue.Host";
    public const string TAuditConductRoot = "src/Cadroue.Conduct";
    public const string TAuditLedgerFile = "TAuditTruthLedger";

    public static readonly string[] TAuditCapsuleInclude =
    [
        "src/Cadroue.UIDeportment.Capsule/*.cs",
    ];

    public static readonly string[] TAuditShellInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
        "src/Cadroue.UIDeportment/*.cs",
        "src/Cadroue.UITerminal/*.cs",
        "src/Cadroue.UIDemeanor/*.cs",
    ];

    public static readonly string[] TAuditTruthInclude =
    [
        "src/Cadroue.UIDeportment/*.cs",
        "src/Cadroue.UIDemeanor/*.cs",
    ];

    public static readonly Dictionary<string, string[]> TAuditTruthInformative = new(StringComparer.Ordinal)
    {
        ["Mismatching"] = ["src/Cadroue.UIDemeanor"],
    };

    public static readonly Dictionary<string, string> TAuditControlPrefix = new(StringComparer.Ordinal)
    {
        ["src/Cadroue.UIDeportment"] = "P",
        ["src/Cadroue.UIDemeanor"] = "P",
    };

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

    public const string TAuditRequestPrefix = "LRequest";

    public static readonly string[] TAuditSendRoots =
    [
        "LTenureRequestApply",
        "LTenureRequestDefer",
    ];

    public static readonly string[] TAuditClockTypes =
    [
        "DispatcherTimer",
        "PeriodicTimer",
        "Stopwatch",
        "Timer",
    ];

    public static readonly string[] TAuditConsoleInput =
    [
        "System.Console.Read",
        "System.Console.ReadKey",
        "System.Console.ReadLine",
        "System.IO.TextReader.Read",
        "System.IO.TextReader.ReadLine",
        "System.IO.TextReader.ReadLineAsync",
        "System.IO.TextReader.ReadToEnd",
    ];

    public static readonly string[] TAuditDialogTypes =
    [
        "System.Windows.MessageBox",
    ];

    public static readonly string[] TAuditDelayMembers =
    [
        "System.Threading.Tasks.Task.Delay",
    ];

    public const string TAuditInputBase = "System.Windows.Controls.Control";

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

    public static readonly string[] TAuditFocusMembers =
    [
        "IsFocused",
        "IsKeyboardFocused",
        "IsKeyboardFocusWithin",
    ];

    public static readonly string[] TAuditTruthHandles = [];

    public static readonly string[] TAuditMoonlightingVerbs =
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
