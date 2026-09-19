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

    private static readonly Regex TAuditCeilingPattern = new(@"\[""(\w+)""\] = (\d+),", RegexOptions.Compiled);

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
    public void AuditRatchet_Enforced_NeverFlipsOff()
    {
        List<string> loosened = [];
        foreach ((string path, bool enforced) in new[]
                 {
                     (TAuditTruthPath, TAuditTruthSetting.TAuditTruthEnforced),
                     (TAuditStrictPath, TAuditStrictSetting.TAuditStrictEnforced),
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
