namespace Convention.Tests;

internal static class TAuditCensusSetting
{
    public const int TAuditGeneration = 24;

    public static readonly IReadOnlyDictionary<string, int> TAuditHollowingFloor = new Dictionary<string, int>
    {
        ["Cadroue.Application"] = 50,
    };

    public static readonly IReadOnlyDictionary<string, string[]> TAuditSquattingPattern =
        new Dictionary<string, string[]>
        {
            ["Cadroue.Core"] = [],
        };

    public static readonly IReadOnlyDictionary<string, string[]> TAuditSmugglingWord =
        new Dictionary<string, string[]>
        {
            ["src/Cadroue.ShellEngine"] = [],
        };
}
