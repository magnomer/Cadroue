using Xunit;

namespace Convention.Tests;

public sealed class TAssayLaundering
{
    [Fact]
    public void AuditTruth_FlagEquals_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    bool ready = QAssayReady(gate);
                    bool same = ready == flag;
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "ready == flag", "Laundering", "logic value in == through 'ready'"));
    }

    [Fact]
    public void AuditTruth_TrueEquals_AllowsComparison()
    {
        TAssayLaunderingCheck("""
                    bool ready = QAssayReady(gate);
                    bool same = ready == true;
            """, null);
    }

    [Fact]
    public void AuditTruth_StaticProperty_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    int level = CAssay.CAssayLimit;
                    bool over = level > items.Length;
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "level > items.Length", "Laundering",
            "logic value in > through 'level'"));
    }

    [Fact]
    public void AuditTruth_EnumMember_AllowsComparison()
    {
        TAssayLaunderingCheck("""
                    int level = (int)CAssayMode.CAssaySecond;
                    bool over = level > items.Length;
            """, null);
    }

    [Fact]
    public void AuditTruth_MixedChain_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    int? count = QAssayCount(gate);
                    if (count is not null && flag)
                    {
                    }
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "if (count is not null && flag)", "Laundering",
            "logic value decides an if through 'count'"));
    }

    [Fact]
    public void AuditTruth_PresenceChain_AllowsDecision()
    {
        TAssayLaunderingCheck("""
                    int? count = QAssayCount(gate);
                    if (count is not null && text is not null)
                    {
                    }
            """, null);
    }

    [Fact]
    public void AuditTruth_PlainOperand_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    int count = QAssayCount(gate);
                    bool named = $"{count}" == text;
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "$\"{count}\" == text", "Laundering",
            "logic value in == through 'count'"));
    }

    [Fact]
    public void AuditTruth_NameofOperand_AllowsComparison()
    {
        TAssayLaunderingCheck("""
                    int count = QAssayCount(gate);
                    bool named = nameof(count) == text;
            """, null);
    }

    [Fact]
    public void AuditTruth_RunText_AllowsDecision()
    {
        TAssayLaunderingCheck("""
                    System.Windows.Documents.Run run = new();
                    if (string.IsNullOrEmpty(run.Text))
                    {
                    }
            """, null);
    }

    [Fact]
    public void AuditTruth_TextBlockText_AllowsDecision()
    {
        TAssayLaunderingCheck("""
                    System.Windows.Controls.TextBlock block = new();
                    if (string.IsNullOrEmpty(block.Text))
                    {
                    }
            """, null);
    }

    [Fact]
    public void AuditTruth_TextBoxText_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    System.Windows.Controls.TextBox box = new();
                    if (string.IsNullOrEmpty(box.Text))
                    {
                    }
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "if (string.IsNullOrEmpty(box.Text))", "Laundering",
            "control input decides an if through 'box'"));
    }

    [Fact]
    public void AuditTruth_ToggleChecked_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    System.Windows.Controls.Primitives.ToggleButton toggle = new();
                    if (toggle.IsChecked == true)
                    {
                    }
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "if (toggle.IsChecked == true)", "Laundering",
            "control input decides an if through 'toggle'"));
    }

    [Fact]
    public void AuditTruth_UnresolvedReceiver_ReportsLaundering()
    {
        TAssayLaunderingCheck("""
                    dynamic shown = new System.Windows.Documents.Run();
                    if (string.IsNullOrEmpty(shown.Text))
                    {
                    }
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "if (string.IsNullOrEmpty(shown.Text))", "Laundering",
            "control input decides an if through 'shown'"));
    }

    private static void TAssayLaunderingCheck(string body, TViolation? expected)
    {
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayTruth.TAssayDriverPath] = TAssayTruth.TAssayDriverFormat("""
                    private static bool QAssayReady(CAssay gate) => gate.CAssayRead() > 0;

                    private static int QAssayCount(CAssay gate) => gate.CAssayRead();
                """, body),
            [TAssayTruth.TAssayGatePath] = """
                namespace Cadroue.Conduct.Assay;

                public enum CAssayMode
                {
                    CAssayFirst,
                    CAssaySecond,
                }

                public sealed class CAssay
                {
                    public static int CAssayLimit { get; } = 1;

                    public int CAssayRead()
                    {
                        return 0;
                    }
                }
                """,
        };
        string driverPath = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayTruth.TAssayDriverPath));
        List<TViolation> hits = TAuditBinder.TAuditAssayRun(sources, () => TAuditLaunderingWalker.TAuditRun(
                [driverPath], TAuditTruthWalker.TAuditReaderRead([driverPath])))
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
        List<TViolation> wanted = expected is null ? [] : [expected];
        Assert.Equal(wanted, hits);
    }
}
