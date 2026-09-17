using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditConvention
{
    public const int TAuditGeneration = 8;

    private readonly ITestOutputHelper TAuditOutput;

    public TAuditConvention(ITestOutputHelper output) => TAuditOutput = output;

    public static string TAuditReportFormat(string audit, string report)
    {
        return $"{audit} GENERATION {TAuditGeneration}\n{report}";
    }

    [Fact]
    public void AuditGate_Veneer_CarriesNoIoOrProcess()
    {
        TAuditGateCheck(
            TAuditGateSetting.TAuditVeneerRoot,
            TAuditGateSetting.TAuditVeneerForbidden,
            TAuditGateSetting.TAuditVeneerKnown);
    }

    [Fact]
    public void AuditGate_Deportment_CarriesNoWpf()
    {
        TAuditGateCheck(
            TAuditGateSetting.TAuditDeportmentRoot,
            TAuditGateSetting.TAuditDeportmentForbidden,
            TAuditGateSetting.TAuditDeportmentKnown);
    }

    private void TAuditGateCheck(string root, string[] forbidden, string[] known)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [root],
            TAuditGateSetting.TAuditGateInclude,
            TAuditGateSetting.TAuditGateSegments,
            [],
            [],
            []);
        string rootFull = Path.Combine(repoRoot, root.Replace('/', Path.DirectorySeparatorChar));

        List<string> fresh = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            string relative = Path.GetRelativePath(rootFull, path).Replace('\\', '/');
            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                string? token = forbidden.FirstOrDefault(
                    entry => lines[index].Contains(entry, StringComparison.Ordinal));
                if (token is null)
                {
                    continue;
                }

                seen.Add(relative);
                if (!known.Contains(relative, StringComparer.Ordinal))
                {
                    fresh.Add($"  {root}/{relative}:{index + 1} {token}");
                }
            }
        }

        string[] cleared = known.Where(entry => !seen.Contains(entry)).ToArray();
        TAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
            "AUDITGATE",
            $"{root}: {seen.Count} known file(s) still carry {string.Join(' ', forbidden)}; "
            + $"{cleared.Length} baseline entr(y/ies) no longer offend and may leave the list."));
        foreach (string entry in cleared)
        {
            TAuditOutput.WriteLine($"  cleared {root}/{entry}");
        }

        Assert.True(fresh.Count == 0, TAuditReportFormat(
            "AUDITGATE",
            $"{fresh.Count} new line(s) in {root} break the Veneer/Deportment gate (C-5, U-VD).\n"
            + string.Join('\n', fresh)));
    }

    [Fact]
    public void AuditConvention_SettingGenerations_MatchTheTooling()
    {
        (string TSidecarName, int TSidecarGeneration)[] sidecars =
        [
            (nameof(TAuditNameRegistry), TAuditNameRegistry.TAuditGeneration),
            (nameof(TAuditNameSetting), TAuditNameSetting.TAuditGeneration),
            (nameof(TAuditLineSetting), TAuditLineSetting.TAuditGeneration),
            (nameof(TAuditCommentSetting), TAuditCommentSetting.TAuditGeneration),
            (nameof(TAuditGateSetting), TAuditGateSetting.TAuditGeneration),
        ];

        string[] stale = sidecars
            .Where(sidecar => sidecar.TSidecarGeneration != TAuditGeneration)
            .Select(sidecar => $"  {sidecar.TSidecarName} is generation {sidecar.TSidecarGeneration}")
            .ToArray();

        Assert.True(stale.Length == 0, TAuditReportFormat(
            "AUDITCONVENTION",
            $"{stale.Length} sidecar(s) differ from this tooling. Rerun the audit that generates each.\n"
            + string.Join('\n', stale)));
    }
}
