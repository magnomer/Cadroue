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
        ["Hydra"] = 7,
        ["Kraken"] = 3,
        ["Spider"] = 1,
        ["Chameleon"] = 63,
        ["Octopus"] = 2,
        ["Centipede"] = 54,
        ["Serpent"] = 17,
        ["Hub"] = 12,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditPartsCeiling = new Dictionary<string, int>
    {
        ["Cadroue.Application.LAudio"] = 2,
        ["Cadroue.Application.LBridge"] = 2,
        ["Cadroue.Application.LCropbox"] = 3,
        ["Cadroue.Application.LEdit"] = 2,
        ["Cadroue.Application.LFix"] = 2,
        ["Cadroue.Application.LNeutral"] = 5,
        ["Cadroue.Application.LPreset"] = 5,
        ["Cadroue.Core.LCapabilityTable"] = 5,
        ["Cadroue.Core.LPiece"] = 2,
        ["Cadroue.Core.LRepertoireCatalog"] = 2,
        ["Cadroue.Infrastructure.LFlyleaf"] = 2,
        ["Cadroue.Infrastructure.LInventory"] = 3,
        ["Cadroue.Infrastructure.LKeyframeOrchestrator"] = 3,
        ["Cadroue.Infrastructure.LMpv"] = 5,
        ["Cadroue.Infrastructure.LScene"] = 2,
        ["Cadroue.Infrastructure.LSchedule"] = 7,
        ["Cadroue.Infrastructure.LSidecarStore"] = 1,
        ["Cadroue.Infrastructure.LTrace"] = 3,
        ["Cadroue.Infrastructure.LTraceWriter"] = 4,
        ["Cadroue.Media.LMedia"] = 5,
        ["Cadroue.ShellEngine.LCartographer"] = 8,
        ["Cadroue.ShellEngine.LEncode"] = 5,
        ["Cadroue.ShellEngine.LEncodeVideo"] = 4,
        ["Cadroue.ShellEngine.LJob"] = 10,
        ["Cadroue.ShellEngine.LMessenger"] = 9,
        ["Cadroue.ShellEngine.LRunner"] = 1,
        ["Cadroue.ShellEngine.LSweep"] = 7,
        ["Cadroue.UIDeportment.QSEncoder"] = 3,
        ["Cadroue.UIVeneer.PHouse.PWindow"] = 5,
        ["Cadroue.UIVeneer.PSDiagnosis"] = 2,
        ["Cadroue.UIVeneer.PSFader"] = 2,
        ["Cadroue.UIVeneer.PSOptions"] = 9,
        ["Cadroue.UIVeneer.PWing.PExport"] = 10,
        ["Cadroue.UIVeneer.PWing.PInspector"] = 24,
        ["Cadroue.UIVeneer.PWing.PSEncoder"] = 17,
        ["Cadroue.UIVeneer.PWing.PSMonitor"] = 2,
    };

    public static readonly string[] TAuditObjectInclude =
    [
        "src/*.cs",
    ];
}
