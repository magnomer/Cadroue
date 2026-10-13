namespace Convention.Tests;

internal static class TAuditCommentSetting
{
    public const int TAuditGeneration = 24;
    public const bool TAuditCommentEnforced = false;
    public const int TAuditCommentWords = 20;
    public const string TAuditCommentPattern = "*.comment.md";
    public const string TAuditCommentRoot = "Cadroue.comment.md";
    public const int TAuditCommentFolderFiles = 10;

    public static readonly string[] TAuditCommentRoots =
    [
        "localization",
        "src",
        "tests",
    ];

    public static readonly string[] TAuditCommentFiles =
    [
        "Directory.Build.props",
        "Cadroue.slnx",
        "version.json",
    ];

    public static readonly string[] TAuditCommentForbidden =
    [
        ";",
    ];

    public static readonly string[] TAuditCommentMarks =
    [
        ".",
        "!",
        "?",
    ];

    public static readonly string[] TAuditCommentSegments =
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

    public static readonly string[] TAuditCommentSources =
    [
        "*.cs",
        "*.xaml",
        "*.csproj",
        "*.json",
        "*.props",
        "*.slnx",
    ];

    public static readonly string[] TAuditCommentSuffixes =
    [
        ".g.cs",
        ".g.i.cs",
        ".AssemblyInfo.cs",
        ".GlobalUsings.g.cs",
        ".Designer.cs",
    ];

    public static readonly string[] TAuditCommentExempt =
    [
        "TAuditNameRegistry.cs",
        "TAuditVerbRegistry.cs",
    ];

    public static readonly string[] TAuditCommentReserved =
    [
        "version.comment.md",
    ];

    public static readonly string[] TAuditCommentAbbreviations =
    [
        "e.g",
        "i.e",
        "etc",
        "vs",
        "cf",
    ];

    public static readonly Dictionary<string, string[]> TAuditCommentMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = ["//", "/*"],
        [".xaml"] = ["<!--"],
        [".props"] = ["<!--"],
        [".csproj"] = ["<!--"],
        [".slnx"] = ["<!--"],
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditCommentCeiling = new Dictionary<string, int>
    {
        ["Unstamped"] = 0,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditCommentFolders =
        new Dictionary<string, int>(StringComparer.Ordinal)
    {
    };

    public static readonly Dictionary<string, string> TAuditCommentClosers = new(StringComparer.Ordinal)
    {
        ["/*"] = "*/",
        ["<!--"] = "-->",
    };
}
