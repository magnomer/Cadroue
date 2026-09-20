namespace Convention.Tests;

internal static class TAuditRingSetting
{
    public const int TAuditGeneration = 10;

    public static readonly IReadOnlyDictionary<string, string[]> TAuditRingEdges = new Dictionary<string, string[]>
    {
        ["Cadroue.Core"] = [],
        ["Cadroue.Application"] = ["Cadroue.Core"],
        ["Cadroue.Infrastructure"] = ["Cadroue.Application", "Cadroue.Core", "Cadroue.Media"],
        ["Cadroue.Media"] = ["Cadroue.Core"],
        ["Cadroue.ShellEngine"] = ["Cadroue.Application", "Cadroue.Core", "Cadroue.Infrastructure", "Cadroue.Media"],
        ["Cadroue.UIDeportment"] =
        [
            "Cadroue.Application",
            "Cadroue.Core",
            "Cadroue.Infrastructure",
            "Cadroue.Media",
            "Cadroue.ShellEngine",
        ],
        ["Cadroue.UIVeneer"] =
        [
            "Cadroue.Application",
            "Cadroue.Core",
            "Cadroue.Infrastructure",
            "Cadroue.Media",
            "Cadroue.ShellEngine",
            "Cadroue.UIDeportment",
        ],
    };
}
