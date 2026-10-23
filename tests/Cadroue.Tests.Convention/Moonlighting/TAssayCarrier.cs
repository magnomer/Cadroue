using Xunit;

namespace Convention.Tests;

public sealed class TAssayCarrier
{
    [Fact]
    public void AuditTruth_LocalControl_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    string field = box.Text;
                    if (field == "a")
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 12, "field", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_LocalPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    string field = text ?? "b";
                    if (field == "a")
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_ParameterControl_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    text = box.Text;
                    if (text == "a")
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 12, "text", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_ParameterPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    text = "b";
                    if (text == "a")
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_LocalFunctionControl_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    Pick(box.Text);

                    void Pick(string field)
                    {
                        if (field == "a")
                        {
                            gate.CAssayApply();
                        }
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "field", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_LocalFunctionPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    Pick("b");

                    void Pick(string field)
                    {
                        if (field == "a")
                        {
                            gate.CAssayApply();
                        }
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_SenderValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { Text: "a" })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "sender", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_SenderType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: string row })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_SenderCase_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    switch (sender)
                    {
                        case System.Windows.Controls.TextBox { Text: "a" }:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "sender", "Misfiring", "control decides a request in a switch case"));
    }

    [Fact]
    public void AuditTruth_SenderTypeCase_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    switch (sender)
                    {
                        case System.Windows.Controls.TextBox { DataContext: string row }:
                            gate.CAssayApply();
                            break;
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_ConstantValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: "a" })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "sender", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_ConstantType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: CAssay })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_EitherValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: "a" or "b" })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "sender", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_EitherType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: CAssay or QAssay })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_NegatedValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: not "a" })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "sender", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_NegatedType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: not CAssay })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_GroupedValue_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: ("a" or "b") })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "sender", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_GroupedType_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox { DataContext: (CAssay or QAssay) })
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }
}
