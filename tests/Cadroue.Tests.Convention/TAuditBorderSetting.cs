namespace Convention.Tests;

internal static class TAuditBorderSetting
{
    public const int TAuditGeneration = 26;
    public const string TAuditBorderHost = "Cadroue.Host";

    public static readonly IReadOnlyDictionary<string, string[]> TAuditBorderNeighbour =
        new Dictionary<string, string[]>
        {
            ["Cadroue.Core"] = [],
            ["Cadroue.Application"] = ["Cadroue.Core"],
            ["Cadroue.ShellEngine"] = ["Cadroue.Application"],
            ["Cadroue.Conduct"] = ["Cadroue.ShellEngine"],
            ["Cadroue.UIDeportment"] = ["Cadroue.Conduct"],
            ["Cadroue.UIVeneer"] = ["Cadroue.UIDeportment"],
            ["Cadroue.UIDemeanor"] = ["Cadroue.Conduct"],
            ["Cadroue.UITerminal"] = ["Cadroue.UIDemeanor"],
            ["Cadroue.Infrastructure"] = ["Cadroue.Core"],
            ["Cadroue.Core.Windows"] = ["Cadroue.Core"],
            ["Cadroue.UIDeportment.Capsule"] = [],
        };

    public static readonly IReadOnlyDictionary<string, string> TAuditBorderCapsule = new Dictionary<string, string>
    {
        ["Cadroue.UIDeportment"] = "Cadroue.UIDeportment.Capsule",
    };

    public static readonly string[] TAuditBorderCut =
    [
        "Cadroue.UIVeneer",
        "Cadroue.UIDeportment",
        "Cadroue.UIDeportment.Capsule",
        "Cadroue.UIDemeanor",
        "Cadroue.UITerminal",
    ];

    public static readonly IReadOnlyDictionary<string, string[]> TAuditOfferPrefix =
        new Dictionary<string, string[]>
        {
            ["Cadroue.Conduct"] = ["C"],
            ["Cadroue.ShellEngine"] = ["L"],
        };

    public static readonly IReadOnlyList<string> TAuditUnsealingRing = ["Cadroue.UIDeportment"];

    public static readonly IReadOnlyDictionary<string, int> TAuditBorderCeiling = new Dictionary<string, int>
    {
        ["Leaking:Cadroue.UIDemeanor>Cadroue.Core"] = 0,
        ["Leaking:Cadroue.UIDemeanor>Cadroue.ShellEngine"] = 0,
        ["Leaking:Cadroue.UIDeportment>Cadroue.Core"] = 0,
        ["Leaking:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 0,
        ["Leapfrogging:Cadroue.Conduct>Cadroue.Core"] = 0,
        ["Leapfrogging:Cadroue.ShellEngine>Cadroue.Core"] = 840,
        ["Trespassing:Cadroue.Infrastructure>Cadroue.Application"] = 71,
        ["Trespassing:Cadroue.ShellEngine>Cadroue.Infrastructure"] = 0,
        ["Trespassing:Cadroue.UIDeportment>Cadroue.Infrastructure"] = 453,
        ["Trespassing:Cadroue.UIVeneer>Cadroue.Infrastructure"] = 103,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.Application"] = 1015,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.Core"] = 1994,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 159,
        ["Undercutting:Cadroue.UIVeneer>Cadroue.Application"] = 271,
        ["Undercutting:Cadroue.UIVeneer>Cadroue.Core"] = 305,
        ["Undercutting:Cadroue.UIVeneer>Cadroue.ShellEngine"] = 6,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.Application"] = 82,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.Core"] = 228,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 11,
    };

    public static readonly string[] TAuditBorderExempt = [];
}
