using Xunit;

namespace Convention.Tests;

public sealed class TAssayRelay
{
    [Fact]
    public void AuditTruth_InjectedDelegate_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck("""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public void QAssayRun(CAssay gate)
                {
                    QAssayRelay relay = new(gate.CAssayApply);
                    relay.QAssayRelayRun();
                }
            }

            public sealed class QAssayRelay
            {
                private readonly Action _send;

                private int _count;

                public QAssayRelay(Action send)
                {
                    _send = send;
                }

                public void QAssayRelayRun()
                {
                    if (_count > 0)
                    {
                        _send();
                    }
                }
            }
            """, "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 28, "QAssayRelay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_InjectedPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck("""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public void QAssayRun(CAssay gate)
                {
                    QAssayRelay relay = new(() => { });
                    relay.QAssayRelayRun();
                }
            }

            public sealed class QAssayRelay
            {
                private readonly Action _send;

                private int _count;

                public QAssayRelay(Action send)
                {
                    _send = send;
                }

                public void QAssayRelayRun()
                {
                    if (_count > 0)
                    {
                        _send();
                    }
                }
            }
            """, "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_DelegateLocal_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    Action send = gate.CAssayApply;
                    if (_count > 0)
                    {
                        send();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_PlainLocal_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    Action send = () => { };
                    if (_count > 0)
                    {
                        send();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_FieldAssigned_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;

                private Action _send = null!;
            """, """
                    _send = gate.CAssayApply;
                    if (_count > 0)
                    {
                        _send();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_FieldInitializer_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;

                private Action _send = new CAssay().CAssayApply;
            """, """
                    if (_count > 0)
                    {
                        _send();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_RelayChain_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    Action first = gate.CAssayApply;
                    Action second = first;
                    if (_count > 0)
                    {
                        second();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 14, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_PlainChain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    Action first = () => { };
                    Action second = first;
                    if (_count > 0)
                    {
                        second();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_AssignedLocal_ReportsGatekeeping()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    Action send;
                    send = gate.CAssayApply;
                    if (_count > 0)
                    {
                        send();
                    }
            """), "Gatekeeping", new TViolation(
            TAssayTruth.TAssayDriverPath, 14, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_AssignedPlain_AllowsRequest()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
            """, """
                    Action send;
                    send = () => { };
                    if (_count > 0)
                    {
                        send();
                    }
            """), "Gatekeeping", null);
    }

    [Fact]
    public void AuditTruth_InterfaceEvent_ReportsGatekeeping()
    {
        TAssayRelayCheck("""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay : IAssayNotice
            {
                private int _count;

                public event Action? IAssayNoticeRaise;

                public void QAssayRun(CAssay gate, IAssayNotice notice)
                {
                    notice.IAssayNoticeRaise += gate.CAssayApply;
                    if (_count > 0)
                    {
                        IAssayNoticeRaise?.Invoke();
                    }
                }
            }
            """, new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "QAssay._count", "Gatekeeping", "decides a request in an if"));
    }

    [Fact]
    public void AuditTruth_PlainEvent_AllowsRequest()
    {
        TAssayRelayCheck("""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay : IAssayNotice
            {
                private int _count;

                public event Action? IAssayNoticeRaise;

                public void QAssayRun(CAssay gate, IAssayNotice notice)
                {
                    notice.IAssayNoticeRaise += () => { };
                    if (_count > 0)
                    {
                        IAssayNoticeRaise?.Invoke();
                    }
                }
            }
            """, null);
    }

    private static void TAssayRelayCheck(string driver, TViolation? expected)
    {
        const string facePath = "src/Cadroue.Conduct/Assay/IAssayNotice.cs";
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayTruth.TAssayDriverPath] = driver,
            [TAssayTruth.TAssayGatePath] = TAssayTruth.TAssayGateText,
            [facePath] = """
                using System;

                namespace Cadroue.Conduct.Assay;

                public interface IAssayNotice
                {
                    event Action? IAssayNoticeRaise;
                }
                """,
        };
        string driverPath = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayTruth.TAssayDriverPath));
        List<TViolation> hits = TAuditBinder.TAuditAssayRun(sources, () => TAuditTruthWalker.TAuditRun([driverPath]))
            .Where(hit => hit.TViolationKind == "Gatekeeping")
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
        List<TViolation> wanted = expected is null ? [] : [expected];
        Assert.Equal(wanted, hits);
    }
}
