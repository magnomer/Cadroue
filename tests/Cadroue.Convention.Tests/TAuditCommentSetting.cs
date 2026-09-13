// Generated file. Do not edit by hand: every line is overwritten when it is rebuilt.
//
// AUDITCOMMENTS GENERATION 8 - settings sidecar for Cadroue.
// One of three generated sidecars in the convention-test project; each carries the values of
// the audit that writes it. Every other file is identical in every project at this generation.

namespace Convention.Tests;

internal static class TAuditCommentSetting
{
    public const int TAuditGeneration = 8;
    public const int TAuditCommentWords = 20;
    public const string TAuditCommentPattern = "*.comment.md";

    public static readonly string[] TAuditCommentRoots =
    [
        "src",
        "tests",
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
        "TAuditCommentSetting.cs",
        "TAuditLineSetting.cs",
        "TAuditNameSetting.cs",
    ];

    public static readonly Dictionary<string, string[]> TAuditCommentMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = ["//", "/*"],
        [".xaml"] = ["<!--"],
    };
}
