namespace Convention.Tests;

internal static class TAuditCommentSetting
{
    public const int TAuditGeneration = 16;
    public const int TAuditCommentWords = 20;
    public const string TAuditCommentPattern = "*.comment.md";

    public static readonly string[] TAuditCommentRoots =
    [
        "localization",
        "src",
        "tests",
    ];

    public static readonly string[] TAuditCommentFiles =
    [
        "Directory.Build.props",
        "Cadroue.sln",
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
        "*.csproj",
        "*.json",
    ];

    public static readonly string[] TAuditCommentOptional =
    [
        "*.cs",
        "*.xaml",
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
    };

    public static readonly Dictionary<string, string> TAuditCommentClosers = new(StringComparer.Ordinal)
    {
        ["/*"] = "*/",
        ["<!--"] = "-->",
    };
}
