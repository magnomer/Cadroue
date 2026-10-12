namespace Convention.Tests;

internal static class TAuditFaultSetting
{
    public const int TAuditGeneration = 22;

    public static readonly string[] TAuditFaultRing =
    [
        "Cadroue.Application",
        "Cadroue.ShellEngine",
    ];

    public static readonly IReadOnlyDictionary<string, int> TAuditFaultCeiling = new Dictionary<string, int>
    {
        ["Cadroue.Application"] = 5,
        ["Cadroue.ShellEngine"] = 15,
    };

    public static readonly string[] TAuditFaultExempt = [];
}
