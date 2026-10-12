namespace Convention.Tests;

internal static class TAuditBorderSetting
{
    public const int TAuditGeneration = 22;
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
        ["Leapfrogging:Cadroue.ShellEngine>Cadroue.Core"] = 832,
        ["Trespassing:Cadroue.Infrastructure>Cadroue.Application"] = 71,
        ["Trespassing:Cadroue.ShellEngine>Cadroue.Infrastructure"] = 0,
        ["Trespassing:Cadroue.UIDeportment>Cadroue.Infrastructure"] = 481,
        ["Trespassing:Cadroue.UIVeneer>Cadroue.Infrastructure"] = 121,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.Application"] = 1269,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.Core"] = 2073,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 157,
        ["Undercutting:Cadroue.UIVeneer>Cadroue.Application"] = 923,
        ["Undercutting:Cadroue.UIVeneer>Cadroue.Core"] = 314,
        ["Undercutting:Cadroue.UIVeneer>Cadroue.ShellEngine"] = 6,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.Application"] = 82,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.Core"] = 229,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 11,
    };

    public static readonly string[] TAuditBorderExempt = [];
}
