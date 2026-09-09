namespace Convention.Tests;

// The registry is read from the generated settings sidecar. Nothing is parsed here: the values are
// compiled in, so the tests read no document and no embedded resource.
internal sealed class TAuditRegistry
{
    public required HashSet<string> TAuditBases { get; init; }
    public required HashSet<string> TAuditVerbs { get; init; }
    public required Dictionary<string, HashSet<string>> TAuditExempt { get; init; }

    // A row grants its name only inside the files it names, so the same word stays a violation
    // everywhere else. "*" grants a mechanism that is universal by spelling, such as a template part.
    public bool TAuditExemptValidate(string name, string sourcePath) =>
        TAuditExempt.TryGetValue(name, out HashSet<string>? files) &&
        (files.Contains("*") || files.Contains(Path.GetFileName(sourcePath)));

    public static TAuditRegistry TAuditLoad() => new()
    {
        TAuditBases = new HashSet<string>(TAuditSetting.TAuditBases, StringComparer.Ordinal),
        TAuditVerbs = new HashSet<string>(TAuditSetting.TAuditVerbs, StringComparer.Ordinal),
        TAuditExempt = TAuditSetting.TAuditExempt.ToDictionary(
            entry => entry.Key,
            entry => new HashSet<string>(entry.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.Ordinal)
    };
}
