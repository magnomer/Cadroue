using Xunit;

namespace Convention.Tests;

public sealed class TAssaySink
{
    [Fact]
    public void AuditTruth_SwitchBreakAfter_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    switch (items.Length)
                    {
                        case 0:
                            if (_count > 0)
                            {
                                break;
                            }

                            gate.CAssayApply();
                            break;
                        case 1:
                            break;
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_SwitchBreakLater_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    switch (items.Length)
                    {
                        case 0:
                            if (_count > 0)
                            {
                                break;
                            }

                            break;
                        case 1:
                            gate.CAssayApply();
                            break;
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_LoopContinueInside_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    foreach (int item in items)
                    {
                        if (_count > 0)
                        {
                            continue;
                        }

                        gate.CAssayApply();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 14, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_LoopContinueAfter_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    foreach (int item in items)
                    {
                        if (_count > 0)
                        {
                            continue;
                        }
                    }

                    gate.CAssayApply();
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_MixedChain_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private string? _text;
            """, """
                    if (_text is null || _text == "a")
                    {
                        gate.CAssayApply();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 12, "QAssay._text", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_PresenceChain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private string? _text;

                private string? _name;
            """, """
                    if (_text is null || _name is null)
                    {
                        gate.CAssayApply();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_ForgivenOperand_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private string? _text;
            """, """
                    if ((_text!) == "a")
                    {
                        gate.CAssayApply();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 12, "QAssay._text", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_ForgivenReceiver_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private string? _text;
            """, """
                    if ((_text!).Length > 0)
                    {
                        gate.CAssayApply();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_PlainField_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    if (_count == items.Length)
                    {
                        gate.CAssayApply();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 12, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_NameofField_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    if (nameof(_count) == text)
                    {
                        gate.CAssayApply();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_RefParameter_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;

                private static void QAssayCheck(ref int count, CAssay gate)
                {
                    if (count > 0)
                    {
                        gate.CAssayApply();
                    }
                }
            """, """
                    QAssayCheck(ref _count, gate);
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 12, "QAssay._count", "Gatekeeping",
            "decides a request in an if through ref 'count'"));
    }

    [Fact]
    public void AuditTruth_ValueParameter_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;

                private static void QAssayCheck(int count, CAssay gate)
                {
                    if (count > 0)
                    {
                        gate.CAssayApply();
                    }
                }
            """, """
                    QAssayCheck(_count, gate);
            """), "Gatekeeping", null);
    }
}
