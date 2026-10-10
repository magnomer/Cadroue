using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Convention.Tests;

internal static class TAuditPlatformAnalyzer
{
    private static readonly string[] TAuditConfigNames = [".editorconfig", ".globalconfig"];

    private static readonly Regex TAuditSectionPattern = new(@"^\s*\[(?<glob>.+)\]\s*$", RegexOptions.Compiled);

    private static readonly Regex TAuditPairPattern = new(
        @"^\s*(?<key>[^=#;]+?)\s*=\s*(?<value>[^#;]*?)\s*$", RegexOptions.Compiled);

    public static bool TAuditAnalyzerCheck(string repoRoot, string project, string source)
    {
        string rule = TAuditPlatformSetting.TAuditPlatformRule;
        List<string> configs = TAuditPlatformFile.TAuditChainRead(repoRoot, source, TAuditConfigNames).ToList();
        List<string> editors = configs.Where(config => config.EndsWith(TAuditConfigNames[0], StringComparison.Ordinal))
            .ToList();
        int top = editors.FindIndex(config => TAuditPlatformFile.TAuditTextRead(config).Any(line =>
            TAuditPairPattern.Match(line) is { Success: true } pair
            && pair.Groups["key"].Value.Equals("root", StringComparison.OrdinalIgnoreCase)
            && pair.Groups["value"].Value.Equals("true", StringComparison.OrdinalIgnoreCase)));
        IEnumerable<string> ordered = configs
            .Where(config => config.EndsWith(TAuditConfigNames[1], StringComparison.Ordinal))
            .Reverse()
            .Concat(editors.Take(top < 0 ? editors.Count : top + 1).Reverse());
        string? level = null;
        foreach (string config in ordered)
        {
            level = TAuditLevelRead(config, source, $"dotnet_diagnostic.{rule}.severity") ?? level;
        }

        XDocument[] documents = TAuditPlatformFile
            .TAuditChainRead(repoRoot, project, TAuditPlatformFile.TAuditImportNames)
            .Append(project)
            .Select(path => XDocument.Load(path))
            .ToArray();
        bool listed = documents.Any(document =>
            TAuditPlatformProject.TAuditListCheck(document, "WarningsAsErrors", rule));
        bool spared = documents.Any(document =>
            TAuditPlatformProject.TAuditListCheck(document, "WarningsNotAsErrors", rule));
        bool every = documents.Any(document => TAuditPlatformProject.TAuditValueRead(document, "TreatWarningsAsErrors")
            .Any(TAuditPlatformProject.TAuditTrueCheck));
        bool escalated = listed || (every && !spared);
        return level switch
        {
            null => escalated,
            _ when level.Equals("error", StringComparison.OrdinalIgnoreCase) => true,
            _ when level.Equals("warning", StringComparison.OrdinalIgnoreCase) => escalated,
            _ => false,
        };
    }

    private static string? TAuditLevelRead(string config, string source, string key)
    {
        string relative = Path.GetRelativePath(Path.GetDirectoryName(config)!, source).Replace('\\', '/');
        bool applies = config.EndsWith(TAuditConfigNames[1], StringComparison.Ordinal);
        string? level = null;
        foreach (string line in TAuditPlatformFile.TAuditTextRead(config))
        {
            Match section = TAuditSectionPattern.Match(line);
            if (section.Success)
            {
                applies = TAuditGlobCheck(section.Groups["glob"].Value, relative);
                continue;
            }

            Match pair = TAuditPairPattern.Match(line);
            if (applies && pair.Success && pair.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                level = pair.Groups["value"].Value;
            }
        }

        return level;
    }

    private static bool TAuditGlobCheck(string glob, string relative)
    {
        string pattern = glob.Contains('/') ? glob.TrimStart('/') : "**/" + glob;
        System.Text.StringBuilder text = new("^");
        for (int index = 0; index < pattern.Length; index++)
        {
            char next = pattern[index];
            if (next == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
            {
                text.Append(index + 2 < pattern.Length && pattern[index + 2] == '/' ? "(?:.*/)?" : ".*");
                index += index + 2 < pattern.Length && pattern[index + 2] == '/' ? 2 : 1;
            }
            else
            {
                text.Append(next switch
                {
                    '*' => "[^/]*",
                    '?' => "[^/]",
                    '{' => "(?:",
                    '}' => ")",
                    ',' => "|",
                    '[' or ']' => next.ToString(),
                    _ => Regex.Escape(next.ToString())
                });
            }
        }

        return Regex.IsMatch(relative, text.Append('$').ToString(), RegexOptions.IgnoreCase);
    }
}
