namespace Convention.Tests;

internal static class TAuditChainSetting
{
    public const int TAuditGeneration = 10;

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

    public static readonly string[] TAuditChainRoot =
    [
        "src/Cadroue.UIVeneer/App.xaml.cs",
    ];

    public static readonly IReadOnlyDictionary<string, string[]> TAuditChainSurface =
        new Dictionary<string, string[]>();

    public static readonly IReadOnlyDictionary<string, int> TAuditChainFloor = new Dictionary<string, int>
    {
        ["Cadroue.Application"] = 40,
    };

    public static readonly IReadOnlyDictionary<string, string[]> TAuditChainStray =
        new Dictionary<string, string[]>();

    public static readonly IReadOnlyDictionary<string, int> TAuditChainCeiling = new Dictionary<string, int>
    {
        ["outward:Cadroue.Infrastructure>Cadroue.Application"] = 9,
        ["outward:Cadroue.Infrastructure>Cadroue.Media"] = 14,
        ["outward:Cadroue.ShellEngine>Cadroue.Infrastructure"] = 23,
        ["outward:Cadroue.ShellEngine>Cadroue.Media"] = 9,
        ["outward:Cadroue.UIDeportment>Cadroue.Infrastructure"] = 54,
        ["outward:Cadroue.UIDeportment>Cadroue.Media"] = 11,
        ["outward:Cadroue.UIVeneer>Cadroue.Infrastructure"] = 27,
        ["outward:Cadroue.UIVeneer>Cadroue.Media"] = 4,
        ["reach:Cadroue.ShellEngine>Cadroue.Core"] = 49,
        ["reach:Cadroue.UIDeportment>Cadroue.Application"] = 69,
        ["reach:Cadroue.UIDeportment>Cadroue.Core"] = 58,
        ["reach:Cadroue.UIVeneer>Cadroue.Application"] = 118,
        ["reach:Cadroue.UIVeneer>Cadroue.Core"] = 28,
        ["reach:Cadroue.UIVeneer>Cadroue.ShellEngine"] = 4,
    };

    public static readonly string[] TAuditChainWaiver = [];
}
