using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Convention.Tests;

internal static class TAuditPlatformPortable
{
    private static readonly Regex TAuditPragmaPattern = new(
        @"^\s*#\s*pragma\s+warning\s+disable\b", RegexOptions.Compiled);

    private static readonly Regex TAuditBarePattern = new(@"disable\s*$", RegexOptions.Compiled);

    public static IEnumerable<TAuditHit> TAuditSuppressRead(
        string repoRoot, string project, IEnumerable<string> held, string name)
    {
        string rule = TAuditPlatformSetting.TAuditPlatformRule;
        XDocument[] documents = TAuditPlatformFile
            .TAuditChainRead(repoRoot, project, TAuditPlatformFile.TAuditImportNames)
            .Append(project)
            .Select(path => XDocument.Load(path))
            .ToArray();
        string relative = TAuditPlatformFile.TAuditRelativeRead(repoRoot, project);
        if (documents.Any(document => TAuditPlatformProject.TAuditListCheck(document, "NoWarn", rule)))
        {
            yield return new TAuditHit(relative, 0, name, "Suppress", rule, $"NoWarn holds {rule}");
        }

        foreach ((string property, string value) in TAuditPlatformSetting.TAuditPlatformSilencers)
        {
            if (documents.Any(document => TAuditPlatformProject.TAuditValueRead(document, property)
                    .Any(found => found.Equals(value, StringComparison.OrdinalIgnoreCase))))
            {
                yield return new TAuditHit(relative, 0, name, "Suppress", property, $"{property} is {value}");
            }
        }

        foreach (string source in held.Where(source => source.EndsWith(".cs", StringComparison.Ordinal)))
        {
            string[] lines = TAuditPlatformFile.TAuditTextRead(source);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                bool pragma = TAuditPragmaPattern.IsMatch(line)
                              && (line.Contains(rule, StringComparison.Ordinal) || TAuditBarePattern.IsMatch(line));
                bool attribute = line.Contains("SuppressMessage", StringComparison.Ordinal)
                                 && line.Contains(rule, StringComparison.Ordinal);
                if (pragma || attribute)
                {
                    yield return new TAuditHit(
                        TAuditPlatformFile.TAuditRelativeRead(repoRoot, source), index + 1, name, "Suppress", rule,
                        line.Trim());
                }
            }
        }
    }

    public static IEnumerable<TAuditHit> TAuditSourceScan(string repoRoot, string source, string project)
    {
        Regex[] patterns = TAuditPlatformSetting.TAuditPlatformPatterns.Select(pattern => new Regex(pattern)).ToArray();
        string relative = TAuditPlatformFile.TAuditRelativeRead(repoRoot, source);
        string[] lines = TAuditPlatformFile.TAuditTextRead(source);
        for (int index = 0; index < lines.Length; index++)
        {
            Match? match = patterns.Select(pattern => pattern.Match(lines[index]))
                .FirstOrDefault(found => found.Success);
            if (match is not null)
            {
                yield return new TAuditHit(
                    relative, index + 1, project, "Windows", match.Value, $"names {match.Value}");
            }
        }
    }
}
