using Xunit;

namespace Convention.Tests;

public sealed class TAssayRelational
{
    [Fact]
    public void AuditTruth_RelationalLabel_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.Text.Length)
                    {
                        case > 3:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_RelationalPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case > 3:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_GuardValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case 1 when box.Text != "a":
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_GuardPresence_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case 1 when box.Text != null:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_TernaryValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Text != "a" ? gate.CAssayRead() : 0;
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in a ternary"));
    }

    [Fact]
    public void AuditTruth_TernaryPresence_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Text != null ? gate.CAssayRead() : 0;
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_RelationalIf_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.Text.Length is > 3) gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_RelationalType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.DataContext is CAssay row) row.CAssayApply();
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_RelationalArm_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Text.Length switch
                    {
                        > 3 => gate.CAssayRead(),
                        _ => 0
                    };
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch arm"));
    }

    [Fact]
    public void AuditTruth_DefaultControl_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.Text)
                    {
                        case "a":
                            break;
                        default:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_DiscardControl_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Text switch
                    {
                        "a" => 0,
                        _ => gate.CAssayRead()
                    };
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch arm"));
    }
}
