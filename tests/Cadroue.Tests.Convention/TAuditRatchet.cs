using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditRatchet
{
    internal const string TAuditRatchetAudit = "AUDITRATCHET";

    private static readonly string TAuditConventionPath =
        TAuditRatchetFile.TAuditSettingFolder + nameof(TAuditConvention) + ".cs";

    private static readonly Regex TAuditGenerationPattern = new(@"TAuditGeneration = (\d+);", RegexOptions.Compiled);

    private static readonly string[] TAuditCeilingSuffixes = ["Ceiling", "Limit", "Ledger"];

    private static readonly string[] TAuditShrinkSuffixes = ["Waiver", "Exempt"];

    [Fact]
    public void AuditRatchet_Settings_NeverLoosen()
    {
        string? committed = TAuditRatchetFile.TAuditCommittedRead(TAuditConventionPath);
        Assert.True(committed is not null, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit, $"{TAuditConventionPath} cannot be read at HEAD, so no setting can be held."));
        Match generation = TAuditGenerationPattern.Match(committed);
        if (!generation.Success
            || int.Parse(generation.Groups[1].Value, CultureInfo.InvariantCulture) != TAuditConvention.TAuditGeneration)
        {
            return;
        }

        IReadOnlyList<string> heads = TAuditRatchetFile.TAuditHeadRead();
        IReadOnlyList<string> tree = TAuditRatchetFile.TAuditTreeRead();
        List<string> loosened = [];
        foreach (string name in heads.Except(tree, StringComparer.Ordinal))
        {
            loosened.Add($"  {name} is committed but gone from the working tree");
        }

        foreach (string name in tree.Except(heads, StringComparer.Ordinal))
        {
            loosened.Add($"  {name} is not committed");
        }

        List<string> shared = tree.Intersect(heads, StringComparer.Ordinal).ToList();
        Dictionary<string, Dictionary<string, List<string>>?> before = TAuditRatchetValue.TAuditValueRead(
            shared.ToDictionary(
                name => name,
                name => TAuditRatchetFile.TAuditCommittedRead(TAuditRatchetFile.TAuditSettingFolder + name)));
        Dictionary<string, Dictionary<string, List<string>>?> after = TAuditRatchetValue.TAuditValueRead(
            shared.ToDictionary(name => name, name => (string?)TAuditRatchetFile.TAuditWorkingRead(name)));
        foreach (string key in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            loosened.AddRange(TAuditLoosenRead(
                key, before.GetValueOrDefault(key), after.GetValueOrDefault(key), before.ContainsKey(key)));
        }

        Assert.True(loosened.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit,
            $"{loosened.Count} setting change(s) loosen a gate without a commit or a generation bump.\n"
            + string.Join('\n', loosened)));
    }

    [Fact]
    public void AuditRatchet_SettingParse_MatchesRuntime()
    {
        Dictionary<string, Dictionary<string, List<string>>?> parsed = TAuditRatchetValue.TAuditValueRead(
            TAuditRatchetFile.TAuditTreeRead()
                .Where(name => name.EndsWith(TAuditRatchetFile.TAuditSettingSuffix, StringComparison.Ordinal))
                .ToDictionary(name => name, name => (string?)TAuditRatchetFile.TAuditWorkingRead(name)));
        List<string> drift = [];
        foreach ((string key, Dictionary<string, List<string>>? value) in parsed.OrderBy(
                     pair => pair.Key, StringComparer.Ordinal))
        {
            string[] parts = key.Split('.');
            FieldInfo? field = typeof(TAuditRatchet).Assembly
                .GetType($"{typeof(TAuditRatchet).Namespace}.{parts[0]}")
                ?.GetField(parts[1], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Dictionary<string, List<string>>? runtime =
                field is null ? null : TAuditRatchetValue.TAuditRuntimeRead(field.GetValue(null));
            if (value is null)
            {
                drift.Add($"  {key} holds an entry the ratchet cannot read");
            }
            else if (runtime is null || TAuditFixedRead(value, runtime).Any())
            {
                drift.Add($"  {key} reads differently from its runtime value");
            }
        }

        Assert.True(drift.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditRatchetAudit,
            $"{drift.Count} setting(s) the ratchet would misread, so a loosening could pass unseen.\n"
            + string.Join('\n', drift)));
    }

    private static IEnumerable<string> TAuditLoosenRead(
        string key,
        Dictionary<string, List<string>>? before,
        Dictionary<string, List<string>>? after,
        bool held)
    {
        if (!held)
        {
            return [$"  {key} is a new setting"];
        }

        if (after is null)
        {
            return [$"  {key} is gone or unreadable"];
        }

        if (before is null)
        {
            return [$"  {key} is unreadable at HEAD"];
        }

        string name = key[(key.IndexOf('.') + 1)..];
        IEnumerable<string> found = TAuditCeilingSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal))
            ? TAuditCeilingRead(before, after, 1)
            : name.EndsWith("Floor", StringComparison.Ordinal) ? TAuditCeilingRead(before, after, -1)
            : TAuditShrinkSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal))
                ? TAuditShrinkRead(before, after)
            : name.EndsWith("Enforced", StringComparison.Ordinal) ? TAuditEnforcedRead(before, after)
            : TAuditFixedRead(before, after);
        return found.Select(entry => $"  {key}{entry}");
    }

    private static IEnumerable<string> TAuditCeilingRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after, int loose)
    {
        foreach (string slot in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            decimal was = TAuditNumberRead(before.GetValueOrDefault(slot));
            decimal now = TAuditNumberRead(after.GetValueOrDefault(slot));
            if (Math.Sign(now - was) == loose)
            {
                yield return $"[{slot}] committed {TAuditNumberFormat(before, slot)}, "
                             + $"now {TAuditNumberFormat(after, slot)}";
            }
        }
    }

    private static IEnumerable<string> TAuditShrinkRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)
    {
        foreach ((string slot, List<string> items) in after.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            List<string> known = before.GetValueOrDefault(slot) ?? [];
            foreach (string item in items.Where(item => !known.Contains(item, StringComparer.Ordinal)))
            {
                yield return $"[{slot}] gained {item}";
            }
        }
    }

    private static IEnumerable<string> TAuditEnforcedRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)
    {
        string was = string.Join(',', before.GetValueOrDefault(string.Empty) ?? []);
        string now = string.Join(',', after.GetValueOrDefault(string.Empty) ?? []);
        if (was == bool.TrueString && now != bool.TrueString)
        {
            yield return " switched enforcement off";
        }
    }

    private static IEnumerable<string> TAuditFixedRead(
        Dictionary<string, List<string>> before, Dictionary<string, List<string>> after)
    {
        foreach (string slot in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            string[] was = [.. (before.GetValueOrDefault(slot) ?? []).Order(StringComparer.Ordinal)];
            string[] now = [.. (after.GetValueOrDefault(slot) ?? []).Order(StringComparer.Ordinal)];
            bool kept = before.ContainsKey(slot) && after.ContainsKey(slot);
            if (!kept || !was.SequenceEqual(now, StringComparer.Ordinal))
            {
                yield return $"[{slot}] changed from [{string.Join(", ", was)}] to [{string.Join(", ", now)}]";
            }
        }
    }

    private static decimal TAuditNumberRead(List<string>? items)
    {
        return items is [string single] && decimal.TryParse(
            single, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : 0;
    }

    private static string TAuditNumberFormat(Dictionary<string, List<string>> values, string slot)
    {
        return values.TryGetValue(slot, out List<string>? items) ? string.Join(',', items) : "absent";
    }
}
