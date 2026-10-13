namespace Convention.Tests;

internal static class TAuditCharterSetting
{
    public const int TAuditGeneration = 25;

    public static readonly IReadOnlyDictionary<string, string[]> TAuditCharterEdges = new Dictionary<string, string[]>
    {
        ["Cadroue.Core"] = [],
        ["Cadroue.Application"] = ["Cadroue.Core"],
        ["Cadroue.Infrastructure"] = ["Cadroue.Core"],
        ["Cadroue.Core.Windows"] = ["Cadroue.Core"],
        ["Cadroue.Host"] =
        [
            "Cadroue.Core", "Cadroue.Application", "Cadroue.Infrastructure", "Cadroue.Core.Windows",
            "Cadroue.ShellEngine", "Cadroue.Conduct", "Cadroue.UIDeportment", "Cadroue.UIVeneer",
            "Cadroue.UIDemeanor", "Cadroue.UITerminal",
        ],
        ["Cadroue.UITerminal"] = ["Cadroue.UIDemeanor"],
        ["Cadroue.UIDemeanor"] = ["Cadroue.Conduct"],
        ["Cadroue.ShellEngine"] = ["Cadroue.Application"],
        ["Cadroue.Conduct"] = ["Cadroue.ShellEngine"],
        ["Cadroue.UIDeportment"] = ["Cadroue.Conduct", "Cadroue.UIDeportment.Capsule"],
        ["Cadroue.UIDeportment.Capsule"] = [],
        ["Cadroue.UIVeneer"] = ["Cadroue.UIDeportment"],
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditCharterCeiling = new Dictionary<string, int>
    {
        ["Piggybacking"] = 2,
    };
}
