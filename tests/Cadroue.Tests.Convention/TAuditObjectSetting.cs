namespace Convention.Tests;

internal static class TAuditObjectSetting
{
    public const int TAuditGeneration = 26;
    public const bool TAuditObjectEnforced = true;
    public const string TAuditObjectReport = "temp/audit/Object-{0}.md";

    public static readonly IReadOnlyDictionary<string, double> TAuditHydraLimit = new Dictionary<string, double>
    {
        ["Parts"] = 5,
        ["Lines"] = 1000,
        ["Fused"] = 0.75,
        ["Density"] = 0.5,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditSpiderLimit = new Dictionary<string, int>
    {
        ["Outgoing"] = 25,
        ["Incoming"] = 25,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditChameleonLimit = new Dictionary<string, int>
    {
        ["Mutable"] = 7,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditOctopusLimit = new Dictionary<string, int>
    {
        ["Outgoing"] = 40,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditCentipedeLimit = new Dictionary<string, int>
    {
        ["Members"] = 40,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditSerpentLimit = new Dictionary<string, int>
    {
        ["Lines"] = 500,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditHubLimit = new Dictionary<string, int>
    {
        ["Parts"] = 5,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditObjectCeiling = new Dictionary<string, int>
    {
        ["Hydra"] = 4,
        ["Kraken"] = 4,
        ["Spider"] = 1,
        ["Chameleon"] = 63,
        ["Octopus"] = 3,
        ["Centipede"] = 54,
        ["Serpent"] = 8,
        ["Hub"] = 8,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditPartsCeiling = new Dictionary<string, int>
    {
        ["Cadroue.Application.LAudio"] = 1,
        ["Cadroue.Application.LBridge"] = 1,
        ["Cadroue.Application.LCropbox"] = 1,
        ["Cadroue.Application.LEdit"] = 1,
        ["Cadroue.Application.LFix"] = 1,
        ["Cadroue.Application.LNeutral"] = 1,
        ["Cadroue.Application.LPreset"] = 1,
        ["Cadroue.Core.LCapabilityTable"] = 1,
        ["Cadroue.Core.LPiece"] = 1,
        ["Cadroue.Core.LRepertoireCatalog"] = 1,
        ["Cadroue.Infrastructure.LFlyleaf"] = 1,
        ["Cadroue.Infrastructure.LInventory"] = 1,
        ["Cadroue.Infrastructure.LKeyframeOrchestrator"] = 1,
        ["Cadroue.Infrastructure.LMpv"] = 1,
        ["Cadroue.Infrastructure.LScene"] = 1,
        ["Cadroue.Infrastructure.LSchedule"] = 1,
        ["Cadroue.Infrastructure.LSidecarStore"] = 1,
        ["Cadroue.Infrastructure.LTrace"] = 1,
        ["Cadroue.Infrastructure.LTraceWriter"] = 1,
        ["Cadroue.Media.LMedia"] = 1,
        ["Cadroue.ShellEngine.LCartographer"] = 1,
        ["Cadroue.ShellEngine.LEncode"] = 1,
        ["Cadroue.ShellEngine.LEncodeVideo"] = 1,
        ["Cadroue.ShellEngine.LJob"] = 1,
        ["Cadroue.ShellEngine.LMessenger"] = 1,
        ["Cadroue.ShellEngine.LRunner"] = 1,
        ["Cadroue.ShellEngine.LSweep"] = 1,
        ["Cadroue.UIDeportment.QSEncoder"] = 3,
        ["Cadroue.UIVeneer.PHouse.PWindow"] = 5,
        ["Cadroue.UIVeneer.PSDiagnosis"] = 2,
        ["Cadroue.UIVeneer.PSFader"] = 2,
        ["Cadroue.UIVeneer.PSOptions"] = 9,
        ["Cadroue.UIVeneer.PWing.PExport"] = 10,
        ["Cadroue.UIVeneer.PWing.PInspector"] = 22,
        ["Cadroue.UIVeneer.PWing.PSEncoder"] = 17,
        ["Cadroue.UIVeneer.PWing.PSMonitor"] = 2,
    };

    public static readonly string[] TAuditObjectInclude =
    [
        "src/*.cs",
    ];
}
