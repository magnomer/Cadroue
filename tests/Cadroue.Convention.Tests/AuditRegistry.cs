using System.Reflection;
using System.Text.Json;

namespace Cadroue.Convention.Tests;

internal sealed class AuditRegistry
{
    public required HashSet<string> Bases { get; init; }
    public required HashSet<string> Verbs { get; init; }
    public required HashSet<string> Exempt { get; init; }
    public required HashSet<string> XInstances { get; init; }

    public static AuditRegistry Load()
    {
        Assembly assembly = typeof(AuditRegistry).Assembly;
        string resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("registry.json", StringComparison.Ordinal));

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded registry '{resourceName}' was not found.");
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;

        return new AuditRegistry
        {
            Bases = ReadSet(root, "bases", StringComparer.Ordinal),
            Verbs = ReadSet(root, "verbs", StringComparer.Ordinal),
            Exempt = ReadSet(root, "exempt", StringComparer.Ordinal),
            XInstances = ReadSet(root, "xinstance", StringComparer.OrdinalIgnoreCase)
        };
    }

    private static HashSet<string> ReadSet(JsonElement root, string property, StringComparer comparer)
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
