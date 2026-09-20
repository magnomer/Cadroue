namespace Convention.Tests;

internal static class TAuditObjectSetting
{
    public const int TAuditGeneration = 10;
    public const bool TAuditObjectEnforced = true;
    public const string TAuditObjectReport = "temp/audit/Object-{0}.md";
    public const int TAuditPartFloor = 5;
    public const int TAuditLineFloor = 1000;
    public const int TAuditHubReach = 5;
    public const double TAuditWeaveFloor = 0.75;
    public const double TAuditDensityFloor = 0.5;

    public static readonly IReadOnlyDictionary<string, int> TAuditObjectCeiling = new Dictionary<string, int>
    {
        ["Monolith"] = 7,
        ["Hub"] = 16,
    };

    public static readonly IReadOnlyDictionary<string, int> TAuditPartCeiling =
        new Dictionary<string, int>();

    public static readonly string[] TAuditObjectInclude =
    [
        "src/*.cs",
    ];
}
