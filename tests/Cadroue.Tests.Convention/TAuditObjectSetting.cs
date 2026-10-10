namespace Convention.Tests;

internal static class TAuditObjectSetting
{
    public const int TAuditGeneration = 21;
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
        ["Hydra"] = 0,
        ["Kraken"] = 0,
        ["Spider"] = 0,
        ["Chameleon"] = 0,
        ["Octopus"] = 0,
        ["Centipede"] = 0,
        ["Serpent"] = 0,
        ["Hub"] = 0,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditPartsCeiling = new Dictionary<string, int>
    {
    };

    public static readonly string[] TAuditObjectInclude =
    [
        "src/*.cs",
    ];
}
