namespace Convention.Tests;

internal static class TAuditPlatformSetting
{
    public const int TAuditGeneration = 16;
    public const bool TAuditPlatformEnforced = false;
    public const string TAuditPlatformRoot = "src/";
    public const string TAuditPlatformPortable = "net10.0";
    public const string TAuditPlatformTwin = "net10.0-windows";
    public const string TAuditPlatformRule = "CA1416";

    public static readonly string[] TAuditPlatformShell = ["Cadroue.UIVeneer"];

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
        ["Cadroue.Application"] = "Cadroue.Application",
        ["Cadroue.Infrastructure"] = "Cadroue.Infrastructure",
        ["Cadroue.Media"] = "Cadroue.Media",
        ["Cadroue.ShellEngine"] = "Cadroue.ShellEngine",
        ["Cadroue.UIDeportment"] = "Cadroue.UIDeportment",
        ["Cadroue.UIVeneer"] = "Cadroue.UIVeneer",
    };

    public static readonly IReadOnlyDictionary<string, string> TAuditPlatformCapsule = new Dictionary<string, string>();

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

    public static readonly IReadOnlyDictionary<string, int> TAuditPlatformCeiling = new Dictionary<string, int>();
}
