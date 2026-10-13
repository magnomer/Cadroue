namespace Convention.Tests;

internal static class TAuditPlatformSetting
{
    public const int TAuditGeneration = 25;
    public const bool TAuditPlatformEnforced = true;
    public const string TAuditPlatformRoot = "src/";
    public const string TAuditPlatformPortable = "net10.0";
    public const string TAuditPlatformTwin = "net10.0-windows";
    public const string TAuditPlatformRule = "CA1416";

    public static readonly string[] TAuditPlatformShell = ["Cadroue.Conduct"];

    public static readonly string[] TAuditPlatformKinds =
    [
        "Unmapped", "Absent", "Framework", "Reference", "Column", "Analyzer", "Windows", "Empty",
        "Suppress", "Implicit", "Domain",
    ];

    public static readonly IReadOnlyDictionary<string, string> TAuditPlatformSilencers =
        new Dictionary<string, string>
        {
            ["AnalysisLevel"] = "none",
            ["AnalysisMode"] = "None",
            ["EnableNETAnalyzers"] = "false",
            ["RunAnalyzers"] = "false",
            ["RunAnalyzersDuringBuild"] = "false",
        };

    public static readonly IReadOnlyDictionary<string, string> TAuditPlatformColumn = new Dictionary<string, string>
    {
        ["Cadroue.Core"] = "Cadroue.Core",
        ["Cadroue.Core.Windows"] = "Cadroue.Core",
        ["Cadroue.Application"] = "Cadroue.Application",
        ["Cadroue.Infrastructure"] = "Cadroue.Infrastructure",
        ["Cadroue.ShellEngine"] = "Cadroue.ShellEngine",
        ["Cadroue.Conduct"] = "Cadroue.Conduct",
        ["Cadroue.UIDeportment"] = "Cadroue.Conduct",
        ["Cadroue.UIDeportment.Capsule"] = "Cadroue.UIDeportment.Capsule",
        ["Cadroue.UIVeneer"] = "Cadroue.Conduct",
        ["Cadroue.UIDemeanor"] = "Cadroue.UIDemeanor",
        ["Cadroue.UITerminal"] = "Cadroue.UITerminal",
        ["Cadroue.Host"] = "",
    };

    public static readonly IReadOnlyDictionary<string, string> TAuditPlatformCapsule = new Dictionary<string, string>
    {
        ["Cadroue.UIDeportment"] = "Cadroue.UIDeportment.Capsule",
    };

    public static readonly string[] TAuditPlatformProperties = ["UseWPF", "UseWindowsForms", "UseWinUI"];

    public static readonly string[] TAuditPlatformPackages =
    [
        "Microsoft.Web.WebView2",
        "Microsoft.WindowsAppSDK",
        "Microsoft.Windows.Compatibility",
        "SharpVectors.Wpf",
    ];

    public static readonly string[] TAuditPlatformPatterns =
    [
        @"\bSystem\.Windows\b",
        @"\bMicrosoft\.Win32\b",
        @"\bWindows\.(Win32|UI|Storage|Media|System|Foundation|Graphics|Devices)\b",
        @"\[\s*(assembly\s*:\s*)?(DllImport|LibraryImport|SupportedOSPlatform)\b",
        @"\bOperatingSystem\.IsWindows\w*\b",
        @"\bRuntimeInformation\.IsOSPlatform\b",
    ];

    public static readonly IReadOnlyDictionary<string, int> TAuditPlatformCeiling = new Dictionary<string, int>
    {
        ["Unmapped"] = 1,
        ["Absent"] = 2,
        ["Framework"] = 0,
        ["Column"] = 2,
        ["Analyzer"] = 4,
        ["Windows"] = 23,
        ["Implicit"] = 7,
    };
}
