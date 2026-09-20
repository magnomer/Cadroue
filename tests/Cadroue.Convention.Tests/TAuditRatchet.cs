using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditRatchet
{
    private static readonly string TAuditSettingFolder =
        $"tests/{TAuditNameSetting.TAuditProject}.Convention.Tests/";

    private static readonly string TAuditTruthPath = TAuditSettingFolder + "TAuditTruthSetting.cs";

    private static readonly string TAuditStrictPath = TAuditSettingFolder + "TAuditStrictSetting.cs";

    private static readonly string TAuditObjectPath = TAuditSettingFolder + "TAuditObjectSetting.cs";

    private static readonly string TAuditChainPath = TAuditSettingFolder + "TAuditChainSetting.cs";

    private static readonly string TAuditFramePath = TAuditSettingFolder + "TAuditFrameSetting.cs";

    private static readonly Regex TAuditCeilingPattern = new(@"\[""([^""]+)""\] = (\d+),", RegexOptions.Compiled);

    private static readonly Regex TAuditRowPattern = new(@"^\s*""([^"":]+:[\w.]+)"",", RegexOptions.Multiline);

    private static readonly Regex TAuditRolePattern =
        new(@"\[""(?<role>[^""]+)""\]\s*=\s*\[(?<names>[^\]]*)\]", RegexOptions.Singleline);

    private static readonly Regex TAuditQuotedPattern = new(@"""([^""]+)""", RegexOptions.Compiled);

    private static readonly Regex TAuditEnforcedPattern = new(@"Enforced = (true|false);", RegexOptions.Compiled);

    private static readonly Regex TAuditGenerationPattern = new(@"TAuditGeneration = (\d+);", RegexOptions.Compiled);

    [Fact]
    public void AuditRatchet_TruthCeiling_NeverRises()
    {
        TAuditCeilingCheck(TAuditTruthPath, TAuditTruthSetting.TAuditTruthCeiling);
    }

    [Fact]
    public void AuditRatchet_StrictCeiling_NeverRises()
    {
        TAuditCeilingCheck(TAuditStrictPath, TAuditStrictSetting.TAuditStrictCeiling);
    }

    [Fact]
    public void AuditRatchet_ObjectCeiling_NeverRises()
    {
        Dictionary<string, int> current = TAuditObjectSetting.TAuditObjectCeiling
            .Concat(TAuditObjectSetting.TAuditPartCeiling)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        TAuditCeilingCheck(TAuditObjectPath, current);
    }

    [Fact]
    public void AuditRatchet_ChainCeiling_NeverRises()
    {
        TAuditCeilingCheck(TAuditChainPath, TAuditChainSetting.TAuditChainCeiling);
    }

    [Fact]
    public void AuditRatchet_FrameCeiling_NeverRises()
    {
        TAuditCeilingCheck(TAuditFramePath, TAuditFrameSetting.TAuditFrameCeiling);
    }

    [Fact]
    public void AuditRatchet_ChainWaiver_NeverGrows()
    {
        TAuditRowCheck(TAuditChainPath, "TAuditChainWaiver", TAuditChainSetting.TAuditChainWaiver, "chain waiver");
    }

    [Fact]
    public void AuditRatchet_FrameWaiver_NeverGrows()
    {
        TAuditRowCheck(TAuditFramePath, "TAuditFrameWaiver", TAuditFrameSetting.TAuditFrameWaiver, "frame waiver");
    }

    [Fact]
    public void AuditRatchet_ChainReach_NeverWidens()
    {
        TAuditListCheck(
            TAuditChainPath, "TAuditChainReach", TAuditChainSetting.TAuditChainReach, "reach", "now reaches");
    }

    [Fact]
    public void AuditRatchet_ChainSurface_NeverWidens()
    {
        TAuditListCheck(
            TAuditChainPath, "TAuditChainSurface", TAuditChainSetting.TAuditChainSurface, "surface", "may now name");
    }

    [Fact]
    public void AuditRatchet_FrameAllowed_NeverWidens()
    {
        string? committed = TAuditCommittedRead(TAuditFramePath);
        if (committed is null || !TAuditGenerationCheck(committed))
        {
            return;
        }

        HashSet<string> before = TAuditQuotedPattern.Matches(TAuditBlockRead(committed, "TAuditFrameAllowed"))
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        List<string> widened = TAuditFrameSetting.TAuditFrameAllowed
            .Where(space => !before.Contains(space))
            .Select(space => $"  {space}")
            .ToList();

        Assert.True(widened.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITRATCHET",
            $"{widened.Count} namespace(s) joined the frame without a commit.\n{string.Join('\n', widened)}"));
    }

    [Fact]
    public void AuditRatchet_Enforced_NeverFlipsOff()
    {
        List<string> loosened = [];
        foreach ((string path, bool enforced) in new[]
                 {
                     (TAuditTruthPath, TAuditTruthSetting.TAuditTruthEnforced),
                     (TAuditStrictPath, TAuditStrictSetting.TAuditStrictEnforced),
                     (TAuditObjectPath, TAuditObjectSetting.TAuditObjectEnforced),
                 })
        {
            string? committed = TAuditCommittedRead(path);
            Match match = committed is null || !TAuditGenerationCheck(committed)
                ? Match.Empty
                : TAuditEnforcedPattern.Match(committed);
            if (match.Success && match.Groups[1].Value == "true" && !enforced)
            {
                loosened.Add($"  {path}");
            }
        }

        Assert.True(loosened.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITRATCHET",
            $"{loosened.Count} setting(s) switched enforcement off.\n{string.Join('\n', loosened)}"));
    }

    private static void TAuditCeilingCheck(string path, IReadOnlyDictionary<string, int> current)
    {
        string? committed = TAuditCommittedRead(path);
        if (committed is null || !TAuditGenerationCheck(committed))
        {
            return;
        }

        List<string> raised = [];
        foreach (Match match in TAuditCeilingPattern.Matches(committed))
        {
            string kind = match.Groups[1].Value;
            int before = int.Parse(match.Groups[2].Value);
            if (current.TryGetValue(kind, out int now) && now > before)
            {
                raised.Add($"  {kind}: committed {before}, now {now}");
            }
        }

        Assert.True(raised.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITRATCHET",
            $"{raised.Count} ceiling(s) raised above the committed value.\n{string.Join('\n', raised)}"));
    }

    private static void TAuditRowCheck(string path, string block, IReadOnlyList<string> rows, string label)
    {
        string? committed = TAuditCommittedRead(path);
        if (committed is null || !TAuditGenerationCheck(committed))
        {
            return;
        }

        HashSet<string> known = TAuditRowPattern.Matches(TAuditBlockRead(committed, block))
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        List<string> added = rows
            .Where(row => !known.Contains(row))
            .Select(row => $"  {row}")
            .ToList();

        Assert.True(added.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITRATCHET",
            $"{added.Count} {label} line(s) not in the committed settings.\n{string.Join('\n', added)}"));
    }

    private static void TAuditListCheck(
        string path, string block, IReadOnlyDictionary<string, string[]> current, string label, string verb)
    {
        string? committed = TAuditCommittedRead(path);
        if (committed is null || !TAuditGenerationCheck(committed))
        {
            return;
        }

        List<string> widened = [];
        foreach (Match match in TAuditRolePattern.Matches(TAuditBlockRead(committed, block)))
        {
            string role = match.Groups["role"].Value;
            HashSet<string> before = TAuditQuotedPattern.Matches(match.Groups["names"].Value)
                .Select(name => name.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);
            foreach (string name in current.GetValueOrDefault(role, []).Where(name => !before.Contains(name)))
            {
                widened.Add($"  {role} {verb} {name}");
            }
        }

        Assert.True(widened.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITRATCHET",
            $"{widened.Count} {label} row(s) widened past the committed settings.\n{string.Join('\n', widened)}"));
    }

    private static string TAuditBlockRead(string committed, string name)
    {
        int start = committed.IndexOf(name + " =", StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        int end = committed.IndexOf("};", start, StringComparison.Ordinal);
        int close = committed.IndexOf("];", start, StringComparison.Ordinal);
        if (close >= 0 && (end < 0 || close < end))
        {
            end = close;
        }

        return end < 0 ? committed[start..] : committed[start..end];
    }

    private static bool TAuditGenerationCheck(string committed)
    {
        Match match = TAuditGenerationPattern.Match(committed);
        return match.Success && int.Parse(match.Groups[1].Value) == TAuditConvention.TAuditGeneration;
    }

    private static string? TAuditCommittedRead(string path)
    {
        ProcessStartInfo info = new("git")
        {
            WorkingDirectory = TAuditSource.TAuditRootRead(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        info.ArgumentList.Add("show");
        info.ArgumentList.Add($"HEAD:{path}");
        using Process? process = Process.Start(info);
        if (process is null)
        {
            return null;
        }

        string output = process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : null;
    }
}
