namespace Convention.Tests;

internal static class TAuditBorderSetting
{
    public const int TAuditGeneration = 21;
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

    public static readonly IReadOnlyDictionary<string, string[]> TAuditUnsealingPrefix =
        new Dictionary<string, string[]>
        {
            ["Cadroue.UIDeportment"] = ["L"],
        };

    public static readonly IReadOnlyDictionary<string, int> TAuditBorderCeiling = new Dictionary<string, int>
    {
        ["Leaking:Cadroue.UIDemeanor>Cadroue.Core"] = 0,
        ["Leaking:Cadroue.UIDemeanor>Cadroue.ShellEngine"] = 0,
        ["Leaking:Cadroue.UIDeportment>Cadroue.Core"] = 0,
        ["Leaking:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 0,
        ["Leapfrogging:Cadroue.Conduct>Cadroue.Core"] = 0,
        ["Leapfrogging:Cadroue.ShellEngine>Cadroue.Core"] = 0,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.Application"] = 0,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.Core"] = 0,
        ["Undercutting:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 0,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.Application"] = 0,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.Core"] = 0,
        ["Unsealing:Cadroue.UIDeportment>Cadroue.ShellEngine"] = 0,
    };

    public static readonly string[] TAuditBorderExempt = [];
}
