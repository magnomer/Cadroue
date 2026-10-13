using Xunit;

namespace Convention.Tests;

public sealed class TAssayTruth
{
    internal const string TAssayDriverPath = "src/Cadroue.UIDeportment/Assay/QAssay.cs";

    internal const string TAssayGatePath = "src/Cadroue.Conduct/Assay/CAssay.cs";

    internal const string TAssayGateText = """
        namespace Cadroue.Conduct.Assay;

        public sealed class CAssay
        {
            public void CAssayApply()
            {
            }

            public int CAssayRead()
            {
                return 0;
            }

            public void CAssayRemove(long? id)
            {
            }
        }
        """;

    [Fact]
    public void AuditTruth_NestedReturn_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        if (items.Length == 0) return;
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 16, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_BareReturn_AllowsSecondSend()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        return;
                    }

                    gate.CAssayApply();
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_LambdaReturn_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        Action stop = () => { return; };
                        stop();
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 17, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_AnonymousReturn_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        Action stop = delegate { return; };
                        stop();
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 17, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_LocalThrow_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        void Fail()
                        {
                            throw new InvalidOperationException();
                        }

                        Fail();
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 21, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_BareThrow_AllowsSecondSend()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        throw new InvalidOperationException();
                    }

                    gate.CAssayApply();
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_ThrowExpression_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        _ = text ?? throw new InvalidOperationException();
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 16, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_InnerLoopBreak_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        foreach (int item in items)
                        {
                            break;
                        }
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 19, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_InnerLoopContinue_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        foreach (int item in items)
                        {
                            continue;
                        }
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 19, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_InnerSwitchBreak_ReportsMisfiring()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    if (flag)
                    {
                        gate.CAssayApply();
                        switch (items.Length)
                        {
                            case 0:
                                break;
                        }
                    }

                    gate.CAssayApply();
            """), "Misfiring", new TViolation(
            TAssayDriverPath, 20, "QAssayRun", "Misfiring", "sends a second request after line 12"));
    }

    [Fact]
    public void AuditTruth_OuterLoopBreak_AllowsSecondSend()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    foreach (int item in items)
                    {
                        if (flag)
                        {
                            gate.CAssayApply();
                            break;
                        }

                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    [Fact]
    public void AuditTruth_OuterLoopContinue_AllowsSecondSend()
    {
        TAssayTruthCheck(TAssayDriverFormat("""
                    foreach (int item in items)
                    {
                        if (flag)
                        {
                            gate.CAssayApply();
                            continue;
                        }

                        gate.CAssayApply();
                    }
            """), "Misfiring", null);
    }

    internal static void TAssayTruthCheck(string driver, string kind, TViolation? expected)
    {
        List<TViolation> hits = TAssayTruthRun(driver).Where(hit => hit.TViolationKind == kind).ToList();
        List<TViolation> wanted = expected is null ? [] : [expected];
        Assert.Equal(wanted, hits);
    }

    internal static IReadOnlyList<TViolation> TAssayTruthRun(string driver)
    {
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayDriverPath] = driver,
            [TAssayGatePath] = TAssayGateText,
        };
        string driverPath = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayDriverPath));
        return TAuditBinder.TAuditAssayRun(sources, () => TAuditTruthWalker.TAuditRun([driverPath]))
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
    }

    internal static string TAssayDriverFormat(string body)
    {
        return $$"""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public void QAssayRun(CAssay gate, bool flag, int[] items, string? text)
                {
            {{body}}
                }
            }
            """;
    }

    internal static string TAssayDriverFormat(string members, string body)
    {
        return $$"""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
            {{members}}

                public void QAssayRun(CAssay gate, bool flag, int[] items, string? text)
                {
            {{body}}
                }
            }
            """;
    }
}
