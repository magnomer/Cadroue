namespace Convention.Tests;

internal static class TAuditDeportmentSetting
{
    public static readonly IReadOnlyDictionary<string, string[]> TAuditDeportmentOffer =
        new Dictionary<string, string[]>
        {
            ["Cadroue.UIDeportment>Cadroue.Conduct"] =
                ["CAtelier", "CUsher", "CLineage", "CLineageAnswer", "CLineageEntry", "CLineageBatch", "CLineageStep"],
        };
}
