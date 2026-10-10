namespace Convention.Tests;

internal static class TAuditPuritySetting
{
    public const int TAuditGeneration = 21;

    public static readonly string[] TAuditPurityRing =
    [
        "Cadroue.Core",
        "Cadroue.Application",
        "Cadroue.ShellEngine",
        "Cadroue.Conduct",
    ];

    public static readonly string[] TAuditPurityFrame =
    [
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Globalization",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Diagnostics.CodeAnalysis",
        "System.Runtime.CompilerServices",
    ];

    public static readonly IReadOnlyDictionary<string, string[]> TAuditPurityExtra = new Dictionary<string, string[]>
    {
        ["Cadroue.Core"] = [],
        ["Cadroue.Application"] = [],
        ["Cadroue.ShellEngine"] = ["System.Runtime.ExceptionServices"],
        ["Cadroue.Conduct"] = ["System.Runtime.ExceptionServices"],
    };

    public static readonly string[] TAuditEavesdroppingMember =
    [
        "System.DateTime.Now",
        "System.DateTime.UtcNow",
        "System.DateTime.Today",
        "System.DateTimeOffset.Now",
        "System.DateTimeOffset.UtcNow",
        "System.Environment",
        "System.Guid.NewGuid",
        "System.Random",
        "System.Console",
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.Path",
        "System.Diagnostics.Process",
        "System.Diagnostics.Stopwatch",
    ];

    public static readonly IReadOnlyDictionary<string, int> TAuditPurityCeiling = new Dictionary<string, int>();

    public static readonly string[] TAuditPurityExempt = [];
}
