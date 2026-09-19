namespace Convention.Tests;

internal static class TAuditLineSetting
{
    public const int TAuditGeneration = 10;
    public const int TAuditLineLimit = 500;

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
