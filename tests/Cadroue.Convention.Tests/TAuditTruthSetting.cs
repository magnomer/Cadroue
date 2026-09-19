namespace Convention.Tests;

internal static class TAuditTruthSetting
{
    public const int TAuditGeneration = 10;
    public const bool TAuditTruthEnforced = true;
    public const string TAuditTruthReport = "temp/audit/Custody-{0}.md";
    public const string TAuditStateSuffix = "State";
    public const string TAuditBulletinType = "LNotice";
    public const string TAuditObserverType = "PObserver";
    public const string TAuditShellAssembly = "Cadroue.Shell";
    public const string TAuditConfiguration = "Debug";
    public const string TAuditReferenceRoot = "src/Cadroue.UIVeneer";

    public static readonly string[] TAuditShellInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
        "src/Cadroue.UIDeportment/*.cs",
        "src/Cadroue.UIShell/*.cs",
    ];

    public static readonly string[] TAuditTruthInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
        "src/Cadroue.UIShell/*.cs",
    ];

    public static readonly string[] TAuditLogicAssemblies =
    [
        "Cadroue.Application",
        "Cadroue.Core",
        "Cadroue.Infrastructure",
        "Cadroue.Media",
        "Cadroue.ShellEngine",
    ];

    public static readonly string[] TAuditFrameworkPacks =
    [
        "Microsoft.NETCore.App",
        "Microsoft.WindowsDesktop.App",
    ];

    public static readonly string[] TAuditReferenceSkip =
    [
        "Cadroue",
        "Cadroue.UIDeportment",
        "Cadroue.UIVeneer",
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

    public static readonly IReadOnlyDictionary<string, int> TAuditTruthCeiling = new Dictionary<string, int>
    {
        ["Argument"] = 572,
        ["Guard"] = 320,
        ["Fork"] = 11,
        ["Mirror"] = 30,
        ["Mutation"] = 130,
        ["Shape"] = 100,
    };
}
