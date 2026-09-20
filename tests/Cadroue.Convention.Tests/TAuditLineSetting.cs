namespace Convention.Tests;

internal static class TAuditLineSetting
{
    public const int TAuditGeneration = 10;
    public const bool TAuditLineEnforced = true;
    public const int TAuditLineLimit = 500;
    public const int TAuditLineWarning = 450;
    public const int TAuditWidthBand = 5;

    public static readonly IReadOnlyDictionary<string, int> TAuditLineCeiling = new Dictionary<string, int>
    {
        ["Length"] = 0,
        ["Width"] = 0,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditWidthLimit = new Dictionary<string, int>
    {
        [".cs"] = 120,
        [".xaml"] = 200,
    };

    public static readonly string[] TAuditLineRoots =
    [
        "src",
        "tests",
    ];

    public static readonly string[] TAuditLineInclude =
    [
        "*.cs",
        "*.csproj",
        "*.xaml",
    ];

    public static readonly string[] TAuditLineSegments =
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
}
