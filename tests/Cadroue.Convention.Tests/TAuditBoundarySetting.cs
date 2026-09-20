namespace Convention.Tests;

internal static class TAuditBoundarySetting
{
    public const int TAuditGeneration = 10;

    public static readonly string[] TAuditBoundaryForbidden =
    [
        @"\busing\s+static\s+" + TAuditNameSetting.TAuditProject + @"\.(?!UIVeneer\b|UIDeportment\b)",
    ];

    public static readonly string[] TAuditBoundaryState = [];

    public static readonly string[] TAuditBoundaryConverter = [];

    public static readonly string[] TAuditBoundaryHidden =
    [
        @"^\s*#\s*if\b",
        @"\bdynamic\b",
        @"\bType\.GetType\s*\(",
        @"\bActivator\.",
        @"\.GetMethods?\s*\(",
        @"\.GetPropert(y|ies)\s*\(",
        @"\.GetFields?\s*\(",
        @"<x:Code\b",
        @"\bEnum\.(Try)?Parse\b",
        @"^\s*(global\s+)?using\s+\w+\s*=",
        @"^\s*extern\s+alias\b",
    ];

    public static readonly string[] TAuditBoundaryLoader = [];

    public const string TAuditBoundaryReflection = @"\bSystem\.Reflection\b";

    public const string TAuditBoundaryTimer = @"\bCancellationTokenSource\b";

    public static readonly string[] TAuditBoundaryHold = [];

    public const string TAuditBoundaryPanel = @"\b(class|struct|record)\s+PS?[A-Z]";
}
