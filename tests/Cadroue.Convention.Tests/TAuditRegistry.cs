namespace Convention.Tests;

internal sealed class TAuditRegistry
{
    public required HashSet<string> TAuditBases { get; init; }
    public required HashSet<string> TAuditVerbs { get; init; }
    public required Dictionary<string, HashSet<string>> TAuditExempt { get; init; }

    public bool TAuditExemptValidate(string name, string sourcePath) =>
        TAuditExempt.TryGetValue(name, out HashSet<string>? files) &&
        (files.Contains("*") || files.Contains(Path.GetFileName(sourcePath)));

    public static TAuditRegistry TAuditLoad() => new()
    {
        TAuditBases = new HashSet<string>(TAuditNameSetting.TAuditBases, StringComparer.Ordinal),
        TAuditVerbs = new HashSet<string>(TAuditNameSetting.TAuditVerbs, StringComparer.Ordinal),
        TAuditExempt = TAuditNameSetting.TAuditExempt.ToDictionary(
            entry => entry.Key,
            entry => new HashSet<string>(entry.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.Ordinal)
    };
}
