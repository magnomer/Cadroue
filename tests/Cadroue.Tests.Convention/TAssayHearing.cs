using Xunit;

namespace Convention.Tests;

public sealed class TAssayHearing
{
    [Fact]
    public void AuditTruth_FocusReturn_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (!box.IsKeyboardFocusWithin)
                    {
                        return;
                    }

                    gate.CAssayApply();
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_EnabledReturn_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (!box.IsEnabled)
                    {
                        return;
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FocusedReturn_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsKeyboardFocusWithin)
                    {
                        return;
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FocusGuard_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox
                        { IsKeyboardFocusWithin: true, DataContext: CAssay row })
                    {
                        row.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_UnfocusedGuard_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    object sender = new System.Windows.Controls.TextBox();
                    if (sender is System.Windows.Controls.TextBox
                        { IsKeyboardFocusWithin: false, DataContext: CAssay row })
                    {
                        row.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "sender", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FocusPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsFocused)
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_EnabledGuard_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsEnabled)
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FocusChain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsKeyboardFocused && flag)
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_FocusEither_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsKeyboardFocused || flag)
                    {
                        gate.CAssayApply();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FocusElse_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsFocused)
                    {
                        gate.CAssayApply();
                    }
                    else
                    {
                        gate.CAssayRead();
                    }
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FocusLater_ReportsMisfiring()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    System.Windows.Controls.TextBox box = new();
                    if (box.IsFocused)
                    {
                        gate.CAssayApply();
                        return;
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "box", "Misfiring", "control decides a request in an if"));
    }
}
