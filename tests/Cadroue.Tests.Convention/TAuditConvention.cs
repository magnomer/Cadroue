using Xunit;

namespace Convention.Tests;

public sealed class TAuditConvention
{
    public const int TAuditGeneration = 21;

    public static string TAuditReportFormat(string audit, string report)
    {
        return $"{audit} - AUDIT GENERATION {TAuditGeneration}\n{report}";
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
            (nameof(TAuditTruthSetting), TAuditTruthSetting.TAuditGeneration),
            (nameof(TAuditStrictSetting), TAuditStrictSetting.TAuditGeneration),
            (nameof(TAuditCharterSetting), TAuditCharterSetting.TAuditGeneration),
            (nameof(TAuditObjectSetting), TAuditObjectSetting.TAuditGeneration),
            (nameof(TAuditBorderSetting), TAuditBorderSetting.TAuditGeneration),
            (nameof(TAuditCensusSetting), TAuditCensusSetting.TAuditGeneration),
            (nameof(TAuditPuritySetting), TAuditPuritySetting.TAuditGeneration),
            (nameof(TAuditBoundarySetting), TAuditBoundarySetting.TAuditGeneration),
            (nameof(TAuditEncodingSetting), TAuditEncodingSetting.TAuditGeneration),
            (nameof(TAuditFakeSetting), TAuditFakeSetting.TAuditGeneration),
            (nameof(TAuditFaultSetting), TAuditFaultSetting.TAuditGeneration),
            (nameof(TAuditPlatformSetting), TAuditPlatformSetting.TAuditGeneration),
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
