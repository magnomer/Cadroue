namespace Convention.Tests;

internal static class TAuditStrictSetting
{
    public const int TAuditGeneration = 10;
    public const bool TAuditStrictEnforced = true;
    public const string TAuditStrictReport = "temp/audit/Truth-{0}.md";

    public static readonly IReadOnlyDictionary<string, int> TAuditStrictCeiling = new Dictionary<string, int>
    {
        ["Storage"] = 103,
        ["Flow"] = 815,
        ["Treat"] = 549,
        ["Reach"] = 0,
        ["Taint"] = 455,
    };

    public static readonly string[] TAuditVeneerInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
    ];

    public static readonly string[] TAuditReachInclude =
    [
        "src/Cadroue.UIVeneer/*.xaml",
        "src/Cadroue.UIShell/*.xaml",
    ];

    public const string TAuditDeportmentNamespace = "Cadroue.UIDeportment";

    public static readonly string[] TAuditReachNamespaces =
    [
        "Cadroue.Application",
        "Cadroue.Core",
        "Cadroue.Infrastructure",
        "Cadroue.Media",
        "Cadroue.ShellEngine",
    ];

    public static readonly string[] TAuditTreatVerbs =
    [
        "Aggregate",
        "All",
        "Any",
        "Average",
        "Concat",
        "Contains",
        "Count",
        "Distinct",
        "DistinctBy",
        "Except",
        "First",
        "FirstOrDefault",
        "GroupBy",
        "GroupJoin",
        "Intersect",
        "Join",
        "Last",
        "LastOrDefault",
        "Max",
        "MaxBy",
        "Min",
        "MinBy",
        "OrderBy",
        "OrderByDescending",
        "Reverse",
        "Select",
        "SelectMany",
        "Single",
        "SingleOrDefault",
        "Skip",
        "SkipWhile",
        "Sum",
        "Take",
        "TakeWhile",
        "ThenBy",
        "ThenByDescending",
        "ToDictionary",
        "ToHashSet",
        "ToLookup",
        "Union",
        "Where",
        "Zip",
    ];
}
