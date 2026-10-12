namespace Convention.Tests;

internal static class TAuditEngineSetting
{
    public static readonly IReadOnlyDictionary<string, string[]> TAuditEngineOffer =
        new Dictionary<string, string[]>
        {
            ["Cadroue.Conduct>Cadroue.ShellEngine"] =
                ["LUsherPort", "LLineagePort", "LLineageAnswer", "LLineageGroup", "LLineageBatch", "LLineageStep"],
        };
}
