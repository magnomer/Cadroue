namespace Convention.Tests;

internal static class TAuditStrictSetting
{
    public const int TAuditGeneration = 11;
    public const bool TAuditStrictEnforced = true;
    public const string TAuditStrictReport = "temp/audit/Strict-{0}.md";

    public static readonly IReadOnlyDictionary<string, int> TAuditStrictCeiling = new Dictionary<string, int>
    {
        ["Storage"] = 1475,
        ["Call"] = 5362,
        ["Engine"] = 1312,
        ["Reach"] = 0,
        ["Trigger"] = 14,
    };

    public static readonly string[] TAuditVeneerInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
    ];

    public static readonly string[] TAuditReachInclude =
    [
        "src/Cadroue.UIVeneer/*.xaml",
        "src/Cadroue.UIShell/*.xaml",
    ];

    public static readonly string[] TAuditDeportmentInclude =
    [
        "src/Cadroue.UIDeportment/*.cs",
    ];

    public const string TAuditDeportmentNamespace = "Cadroue.UIDeportment";

    public static readonly string[] TAuditCatalogPatterns =
    [
        @"^\s*using\s+System\.IO\s*;",
        @"^\s*using\s+System\.Text\.Json",
        @"^\s*using\s+System\.Text\.RegularExpressions",
        @"^\s*using\s+System\.Diagnostics\s*;",
        @"\bJsonSerializer\b",
        @"\bJsonDocument\b",
        @"\bRegex\b",
        @"\bProcess\.Start\b",
        @"\bProcessStartInfo\b",
        @"\bTask\.Run\b",
        @"\bFile\.\w+\(",
        @"\bDirectory\.\w+\(",
        @"\bPath\.\w+\(",
    ];

    public static readonly string[] TAuditCatalogExempt =
    [
        "PIcon.cs",
        "PLogWindow.cs",
        "PPlayerFlyleaf.cs",
        "PSAbout.cs",
        "PSOptionsSystemWorkspace.cs",
    ];

    public static readonly string[] TAuditMarkupPatterns =
    [
        @"\bSystem\.Windows\b",
        @"\bDispatcher\b",
        @"\bSystem\.IO\b",
    ];

    public static readonly string[] TAuditMarkupExempt =
    [
        "LAction.cs",
        "LSMonitor.cs",
        "LViewerMedia.cs",
        "LViewerMpv.cs",
        "LViewerSource.cs",
    ];

    public static readonly string[] TAuditReachNamespaces =
    [
        "Cadroue.Application",
        "Cadroue.Core",
        "Cadroue.Infrastructure",
        "Cadroue.Media",
        "Cadroue.ShellEngine",
    ];

    public static readonly string[] TAuditTriggerElements =
    [
        "DataTrigger",
        "MultiDataTrigger",
        "MultiTrigger",
        "Trigger",
    ];

    public static readonly string[] TAuditTriggerSlots =
    [
        "Converter",
        "FallbackValue",
        "StringFormat",
        "TargetNullValue",
    ];
}
