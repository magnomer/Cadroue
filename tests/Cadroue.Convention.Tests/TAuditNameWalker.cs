using System.Text.RegularExpressions;

namespace Convention.Tests;

internal static class TAuditNameWalker
{
    private static readonly Regex TAuditComponentPattern = new(
        TAuditNameSetting.TAuditComponentPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> TAuditMethodKinds =
        new(TAuditNameSetting.TAuditMethodKinds, StringComparer.Ordinal);

    private static readonly HashSet<string> TAuditDataKinds =
        new(TAuditNameSetting.TAuditDataKinds, StringComparer.Ordinal);

    public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> sourcePaths, TAuditRegistry registry)
    {
        List<TSpecimen> candidates = [];
        foreach (string path in sourcePaths)
        {
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                TSpecimenWalker.TSpecimenCodeRead(path, candidates);
            }
            else if (path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                TSpecimenWalker.TSpecimenMarkupRead(path, candidates);
            }
        }

        bool anyTestPrefixed = candidates.Any(candidate =>
            string.Equals(candidate.TSpecimenKind, "TestMethod", StringComparison.Ordinal) &&
            string.Equals(
                TAuditPrefixRead(candidate.TSpecimenName.TrimStart('_')),
                TAuditNameSetting.TAuditTestPrefix,
                StringComparison.OrdinalIgnoreCase));

        List<TViolation> violations = [];
        foreach (TSpecimen candidate in candidates)
        {
            if (registry.TAuditExemptValidate(candidate.TSpecimenName, candidate.TSpecimenPath))
            {
                continue;
            }

            string? reason = TViolationResolve(
                candidate.TSpecimenName, candidate.TSpecimenKind, registry, anyTestPrefixed);
            if (reason is not null)
            {
                violations.Add(new TViolation(
                    candidate.TSpecimenPath,
                    candidate.TSpecimenLine,
                    candidate.TSpecimenName,
                    candidate.TSpecimenKind,
                    reason));
            }
        }

        return violations;
    }

    private static string? TViolationResolve(string name, string kind, TAuditRegistry registry, bool anyTestPrefixed)
    {
        if (string.Equals(kind, "TestMethod", StringComparison.Ordinal))
        {
            if (!anyTestPrefixed)
            {
                return null;
            }

            string? testPrefix = TAuditPrefixRead(name.TrimStart('_'));
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
        string? prefix = TAuditPrefixRead(working);
        if (prefix is null)
        {
            return "missing required prefix";
        }

        string remainder = working[prefix.Length..];
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return null;
        }

        List<string> components = [];
        foreach (string segment in remainder.Split('_'))
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return null;
            }

            MatchCollection matches = TAuditComponentPattern.Matches(segment);
            string rebuilt = string.Concat(matches.Cast<Match>().Select(match => match.Value));
            if (!string.Equals(rebuilt, segment, StringComparison.Ordinal))
            {
                return null;
            }

            components.AddRange(matches.Cast<Match>().Select(match => match.Value));
        }

        if (components.Count == 0)
        {
            return null;
        }

        string baseName = components[0];
        if (!registry.TAuditBases.Contains(baseName))
        {
            return $"unregistered base `{baseName}`";
        }

        string last = components[^1];
        bool lastIsVerb = registry.TAuditVerbs.Contains(last);
        if (TAuditMethodKinds.Contains(kind) && !lastIsVerb)
        {
            return $"method does not end in a registered verb (`{last}`)";
        }

        if (TAuditDataKinds.Contains(kind) && lastIsVerb)
        {
            return $"data or type name ends in a registered verb (`{last}`)";
        }

        if (components.Count > TAuditNameSetting.TAuditComponentLimit)
        {
            return $"{components.Count} components after the prefix " +
                   $"(limit is {TAuditNameSetting.TAuditComponentLimit})";
        }

        return null;
    }

    private static string? TAuditPrefixRead(string name)
    {
        foreach (string prefix in TAuditNameSetting.TAuditPrefixes)
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length)
            {
                continue;
            }

            char next = name[prefix.Length];
            if (char.IsUpper(next) || char.IsDigit(next) || next == '_')
            {
                return prefix;
            }
        }

        return null;
    }
}
