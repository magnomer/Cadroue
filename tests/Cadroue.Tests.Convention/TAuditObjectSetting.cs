namespace Convention.Tests;

internal static class TAuditObjectSetting
{
    public const int TAuditGeneration = 16;
    public const bool TAuditObjectEnforced = true;
    public const string TAuditObjectReport = "temp/audit/Object-{0}.md";
    public const int TAuditPartLimit = 5;
    public const int TAuditSpanLimit = 1000;
    public const int TAuditHubReach = 5;
    public const double TAuditWeaveLimit = 0.75;
    public const double TAuditDensityLimit = 0.5;
    public const int TAuditLargeLines = 500;
    public const int TAuditLargeMembers = 40;
    public const int TAuditLargeState = 12;

    public static readonly IReadOnlyDictionary<string, int> TAuditObjectCeiling = new Dictionary<string, int>
    {
        ["Monolith"] = 7,
        ["Hub"] = 11,
        ["Large"] = 76,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditPartCeiling = new Dictionary<string, int>
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
        ["Cadroue.Infrastructure.LSidecarStore"] = 2,
        ["Cadroue.Infrastructure.LTrace"] = 3,
        ["Cadroue.Infrastructure.LTraceWriter"] = 4,
        ["Cadroue.Media.LMedia"] = 5,
        ["Cadroue.ShellEngine.LCartographer"] = 8,
        ["Cadroue.ShellEngine.LEncode"] = 5,
        ["Cadroue.ShellEngine.LEncodeVideo"] = 4,
        ["Cadroue.ShellEngine.LJob"] = 10,
        ["Cadroue.ShellEngine.LMessenger"] = 9,
        ["Cadroue.ShellEngine.LRunner"] = 3,
        ["Cadroue.ShellEngine.LSweep"] = 7,
        ["Cadroue.UIDeportment.LSEncoder"] = 3,
        ["Cadroue.UIVeneer.PHouse.PWindow"] = 5,
        ["Cadroue.UIVeneer.PSDiagnosis"] = 2,
        ["Cadroue.UIVeneer.PSFader"] = 2,
        ["Cadroue.UIVeneer.PSOptions"] = 9,
        ["Cadroue.UIVeneer.PWing.PExport"] = 10,
        ["Cadroue.UIVeneer.PWing.PInspector"] = 25,
        ["Cadroue.UIVeneer.PWing.PSEncoder"] = 17,
        ["Cadroue.UIVeneer.PWing.PSMonitor"] = 2,
    };

    public static readonly string[] TAuditObjectInclude =
    [
        "src/*.cs",
    ];
}
