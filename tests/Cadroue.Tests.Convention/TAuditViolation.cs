using System.Text.RegularExpressions;

namespace Convention.Tests;

internal static class TAuditViolation
{
    private static readonly Regex TAuditComponentPattern = new(
        TAuditNameSetting.TAuditComponentPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> TAuditMethodKinds =
        new(TAuditNameSetting.TAuditMethodKinds, StringComparer.Ordinal);

    private static readonly HashSet<string> TAuditDataKinds =
        new(TAuditNameSetting.TAuditDataKinds, StringComparer.Ordinal);

    public static string? TViolationResolve(string name, string kind, TAuditRegistry registry, bool anyTestPrefixed)
    {
        if (string.Equals(kind, "TestMethod", StringComparison.Ordinal))
        {
            if (!anyTestPrefixed)
            {
                return null;
            }

            string? testPrefix = TAuditNameFilter.TAuditPrefixRead(name.TrimStart('_'));
            if (testPrefix is null)
            {
                return "test method carries no prefix while other test methods use the " +
                       $"{TAuditNameSetting.TAuditTestPrefix} prefix";
            }

            if (!string.Equals(testPrefix, TAuditNameSetting.TAuditTestPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return $"test method must use the {TAuditNameSetting.TAuditTestPrefix} prefix, not `{testPrefix}`";
            }

            return null;
        }

        string working = name.TrimStart('_');
        string? prefix = TAuditNameFilter.TAuditPrefixRead(working);
        if (prefix is null)
        {
            return "missing required prefix";
        }

        string remainder = working[prefix.Length..];
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return "no base after the prefix";
        }

        List<string> components = [];
        foreach (string segment in remainder.Split('_'))
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return "an underscore leaves an empty component";
            }

            MatchCollection matches = TAuditComponentPattern.Matches(segment);
            string rebuilt = string.Concat(matches.Cast<Match>().Select(match => match.Value));
            if (!string.Equals(rebuilt, segment, StringComparison.Ordinal))
            {
                return $"`{segment}` does not split into PascalCase components";
            }

            components.AddRange(matches.Cast<Match>().Select(match => match.Value));
        }

        if (components.Count == 0)
        {
            return "no base after the prefix";
        }

        List<string> reasons = [];
        string baseName = components[0];
        if (!registry.TAuditBases.Contains(baseName))
        {
            reasons.Add($"unregistered base `{baseName}`");
        }

        string last = components[^1];
        bool lastIsVerb = registry.TAuditVerbs.Contains(last);
        if (TAuditMethodKinds.Contains(kind) && !lastIsVerb)
        {
            reasons.Add($"method does not end in a registered verb (`{last}`)");
        }
        else if (TAuditDataKinds.Contains(kind) && lastIsVerb)
        {
            reasons.Add($"data or type name ends in a registered verb (`{last}`)");
        }

        if (components.Count > TAuditNameSetting.TAuditComponentLimit)
        {
            reasons.Add($"{components.Count} components after the prefix " +
                        $"(limit is {TAuditNameSetting.TAuditComponentLimit})");
        }

        return reasons.Count == 0 ? null : string.Join(", ", reasons);
    }
}
