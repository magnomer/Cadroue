namespace Convention.Tests;

internal static class TAuditChainSetting
{
    public const int TAuditGeneration = 16;
    public const string TAuditChainHost = "Cadroue.Host";

    public static readonly IReadOnlyDictionary<string, string[]> TAuditChainReach = new Dictionary<string, string[]>
    {
        ["Cadroue.Core"] = [],
        ["Cadroue.Application"] = ["Cadroue.Core"],
        ["Cadroue.ShellEngine"] = ["Cadroue.Application"],
        ["Cadroue.UIDeportment"] = ["Cadroue.ShellEngine"],
        ["Cadroue.UIVeneer"] = ["Cadroue.UIDeportment"],
        ["Cadroue.Infrastructure"] = ["Cadroue.Core"],
        ["Cadroue.Media"] = ["Cadroue.Core"],
    };

    public static readonly IReadOnlyDictionary<string, string> TAuditChainCapsule = new Dictionary<string, string>();

    public static readonly string[] TAuditChainCut =
    [
        "Cadroue.UIVeneer",
        "Cadroue.UIDeportment",
    ];

    public static readonly IReadOnlyDictionary<string, string[]> TAuditChainSurface =
        new Dictionary<string, string[]>();

    public static readonly IReadOnlyDictionary<string, string[]> TAuditSealPrefix =
        new Dictionary<string, string[]>();

    public static readonly IReadOnlyDictionary<string, int> TAuditChainFloor = new Dictionary<string, int>
    {
        ["Cadroue.Application"] = 40,
    };

    public static readonly IReadOnlyDictionary<string, string[]> TAuditChainStray =
        new Dictionary<string, string[]>();

    public static readonly IReadOnlyDictionary<string, string[]> TAuditChainBanned =
        new Dictionary<string, string[]>();

    public static readonly IReadOnlyDictionary<string, int> TAuditChainCeiling = new Dictionary<string, int>
    {
        ["cross:Cadroue.UIDeportment>Cadroue.Application"] = 1272,
        ["cross:Cadroue.UIDeportment>Cadroue.Core"] = 2172,
        ["cross:Cadroue.UIVeneer>Cadroue.Application"] = 957,
        ["cross:Cadroue.UIVeneer>Cadroue.Core"] = 325,
        ["cross:Cadroue.UIVeneer>Cadroue.ShellEngine"] = 9,
        ["outward:Cadroue.Infrastructure>Cadroue.Application"] = 79,
        ["outward:Cadroue.Infrastructure>Cadroue.Media"] = 80,
        ["outward:Cadroue.ShellEngine>Cadroue.Infrastructure"] = 91,
        ["outward:Cadroue.ShellEngine>Cadroue.Media"] = 75,
        ["outward:Cadroue.UIDeportment>Cadroue.Infrastructure"] = 534,
        ["outward:Cadroue.UIDeportment>Cadroue.Media"] = 110,
        ["outward:Cadroue.UIVeneer>Cadroue.Infrastructure"] = 137,
        ["reach:Cadroue.ShellEngine>Cadroue.Core"] = 836,
    };

    public static readonly string[] TAuditChainWaiver = [];
}
