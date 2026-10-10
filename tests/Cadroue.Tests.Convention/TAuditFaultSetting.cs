namespace Convention.Tests;

internal static class TAuditFaultSetting
{
    public const int TAuditGeneration = 21;

    public static readonly string[] TAuditFaultRing =
    [
        "Cadroue.Application",
        "Cadroue.ShellEngine",
    ];

    public static readonly IReadOnlyDictionary<string, int> TAuditFaultCeiling = new Dictionary<string, int>
    {
        ["Cadroue.Application"] = 10,
        ["Cadroue.ShellEngine"] = 19,
    };

    public static readonly string[] TAuditFaultExempt =
    [
        "src/Cadroue.Application/Workspace/LWorkspaceClerk.cs:LWorkspacePostureRead",
        "src/Cadroue.Application/Workspace/LWorkspaceClerk.cs:LWorkspaceFallbackSave",
    ];
}
