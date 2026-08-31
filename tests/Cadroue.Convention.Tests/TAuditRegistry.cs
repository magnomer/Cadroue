using System.Reflection;
using System.Text.Json;

namespace Cadroue.Convention.Tests;

internal sealed class TAuditRegistry
{
    public required HashSet<string> TAuditBases { get; init; }
    public required HashSet<string> TAuditVerbs { get; init; }

    public static TAuditRegistry TAuditLoad()
    {
        Assembly assembly = typeof(TAuditRegistry).Assembly;
        string resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("registry.json", StringComparison.Ordinal));

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded registry '{resourceName}' was not found.");
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;

        return new TAuditRegistry
        {
            TAuditBases = TAuditSetRead(root, "bases", StringComparer.Ordinal),
            TAuditVerbs = TAuditSetRead(root, "verbs", StringComparer.Ordinal)
        };
    }

    private static HashSet<string> TAuditSetRead(JsonElement root, string property, StringComparer comparer)
    {
        HashSet<string> values = new(comparer);
        if (root.TryGetProperty(property, out JsonElement array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                string? value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }
        }

        return values;
    }
}
