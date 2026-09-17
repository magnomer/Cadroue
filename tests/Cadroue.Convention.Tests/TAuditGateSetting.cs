namespace Convention.Tests;

internal static class TAuditGateSetting
{
    public const int TAuditGeneration = 8;
    public const string TAuditVeneerRoot = "src/Cadroue.UIVeneer";
    public const string TAuditDeportmentRoot = "src/Cadroue.UIDeportment";

    public static readonly string[] TAuditGateInclude =
    [
        "*.cs",
    ];

    public static readonly string[] TAuditGateSegments =
    [
        ".git",
        ".vs",
        "artifacts",
        "bin",
        "node_modules",
        "obj",
        "packages",
        "publish",
    ];

    public static readonly string[] TAuditVeneerForbidden =
    [
        @"using\s+System\.IO\b",
        @"System\.IO\.",
        @"\b(File|Directory|FileInfo|DirectoryInfo)\.",
        @"\b(FileStream|StreamReader|StreamWriter)\b",
        @"\bProcess\b",
        @"Task\.Run\b",
        @"\b(JsonSerializer|JsonConvert|JsonDocument)\b",
        @"\bRegex\b",
    ];

    public static readonly string[] TAuditVeneerTolerated =
    [
        "System.IO.Path.GetFileName(",
        "System.IO.Path.GetFileNameWithoutExtension(",
        "System.IO.Path.GetExtension(",
        "System.IO.Path.GetFullPath(",
        "System.IO.Path.Combine(",
        "System.IO.IOException",
    ];

    public static readonly (string TAuditFile, string TAuditSpelling)[] TAuditVeneerScoped =
    [
        ("App.xaml.cs", "System.IO.StreamReader"),
        ("PAsset/PIcon.cs", "System.IO.Stream "),
    ];

    public static readonly string[] TAuditDeportmentForbidden =
    [
        @"using\s+System\.Windows\b",
        @"System\.Windows\.",
        @"\bDispatcher\b",
        @"using\s+System\.IO\b",
        @"System\.IO\.",
        @"\b(File|Directory|Path|FileInfo|DirectoryInfo)\.",
        @"\b(FileStream|StreamReader|StreamWriter)\b",
    ];

    public static readonly string[] TAuditDeportmentTolerated =
    [
        "System.IO.Path.GetFileName(",
    ];

    public static readonly string[] TAuditScalarType =
    [
        "bool",
        "int",
        "long",
        "uint",
        "short",
        "double",
        "float",
        "string",
        "TimeSpan",
        "Guid",
    ];

    public static readonly string[] TAuditScalarSuffix =
    [
        "Origin",
        "Offset",
        "Press",
        "Grab",
        "Point",
        "Active",
        "Direction",
        "State",
        "Armed",
        "Gone",
    ];

    public static readonly (string TAuditFile, string TAuditField)[] TAuditScalarKnown =
    [
        ("PFlow/PMap.cs", "pMapGlyphCount"),
        ("PFlow/PMap.cs", "pMapBadgeDpi"),
        ("PFlow/PViewfinder.cs", "pViewfinderGlyphCount"),
        ("PFlow/PViewfinder.cs", "pViewfinderTextDpi"),
    ];

    public static readonly string[] TAuditScalarExempt =
    [
        "App.xaml.cs",
    ];

    public const string TAuditGuardPattern =
        @"\w*(Suppress|Busy|Loading|Rebuilding|Syncing|Applying|Restoring)\w*";
}
