using Xunit;

namespace Convention.Tests;

public sealed class TAssayMisfiring
{
    [Fact]
    public void AuditTruth_SwitchGuardControl_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case 1 when box.IsEnabled:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_SwitchGuardFlag_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case 1 when flag:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_SwitchConstantLabel_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.Text)
                    {
                        case "a":
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_SwitchTypeLabel_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.DataContext)
                    {
                        case string row:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_SwitchNestedConstant_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.Parent)
                    {
                        case System.Windows.Controls.TextBox { Text: "a" }:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_SwitchNestedType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.Parent)
                    {
                        case System.Windows.Controls.TextBox { DataContext: string row }:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_GuardNestedType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case 1 when box.Parent is System.Windows.Controls.TextBox { DataContext: string row }:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_IfNestedConstant_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.Parent is System.Windows.Controls.TextBox { Text: "a" })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_IfNestedType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.Parent is System.Windows.Controls.TextBox { DataContext: string row })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_TernaryNestedConstant_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Parent is System.Windows.Controls.TextBox { Text: "a" }
                        ? gate.CAssayRead()
                        : 0;
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in a ternary"));
    }

    [Fact]
    public void AuditTruth_TernaryNestedType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Parent is System.Windows.Controls.TextBox { DataContext: string row }
                        ? gate.CAssayRead()
                        : 0;
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_GuardNestedConstant_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (items.Length)
                    {
                        case 1 when box.Parent is System.Windows.Controls.TextBox { Text: "a" }:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_ArmConstant_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Text switch
                    {
                        "a" => gate.CAssayRead(),
                        _ => 0
                    };
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "box", "Misfiring", "control decides a request in a switch arm"));
    }

    [Fact]
    public void AuditTruth_ArmType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.DataContext switch
                    {
                        CAssay row => row.CAssayRead(),
                        _ => 0
                    };
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_ArmDiscard_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    int answer = box.Text switch
                    {
                        _ => gate.CAssayRead()
                    };
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_SwitchDefault_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    switch (box.Text)
                    {
                        default:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }
}
