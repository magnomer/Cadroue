using Xunit;

namespace Convention.Tests;

public sealed class TAssayOrigin
{
    private const string TAssayCapsulePath = "src/Cadroue.UIDeportment.Capsule/Assay/LAssayCapsule.cs";

    private const string TAssayCapsuleText = """
        namespace Cadroue.UIDeportment.Capsule.Assay;

        public sealed class LAssayCapsule
        {
            public int LAssayCapsuleRead(int key)
            {
                return key;
            }
        }
        """;

    [Fact]
    public void AuditTruth_PlainDependency_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly QAssayDial _dial = new();

        private sealed class QAssayDial : System.Windows.DependencyObject
        {
            internal static readonly System.Windows.DependencyProperty QAssayDialCount =
                System.Windows.DependencyProperty.Register("QAssayDialCount", typeof(int), typeof(QAssayDial));

            internal void QAssayDialApply(CAssay gate)
            {
                SetValue(QAssayDialCount, gate.CAssayRead());
                SetValue(QAssayDialCount, 3);
            }
        }
    """, """
            _count = gate.CAssayRead();
            _count = (int)_dial.GetValue(QAssayDial.QAssayDialCount);
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 26, "QAssay._count", "Contesting",
            "written by the engine at line 25 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineDependency_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly QAssayDial _dial = new();

        private sealed class QAssayDial : System.Windows.DependencyObject
        {
            internal static readonly System.Windows.DependencyProperty QAssayDialCount =
                System.Windows.DependencyProperty.Register("QAssayDialCount", typeof(int), typeof(QAssayDial));

            internal void QAssayDialApply(CAssay gate)
            {
                SetValue(QAssayDialCount, gate.CAssayRead());
                SetValue(QAssayDialCount, gate.CAssayRead());
            }
        }
    """, """
            _count = gate.CAssayRead();
            _count = (int)_dial.GetValue(QAssayDial.QAssayDialCount);
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_PlainGetter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly int _total = 3;

        private int QAssayTotal
        {
            get
            {
                return _total;
            }
        }
    """, """
            _count = gate.CAssayRead();
            _count = QAssayTotal;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 22, "QAssay._count", "Contesting",
            "written by the engine at line 21 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineGetter_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly int _total = new CAssay().CAssayRead();

        private int QAssayTotal
        {
            get
            {
                return _total;
            }
        }
    """, """
            _count = gate.CAssayRead();
            _count = QAssayTotal;
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_PlainParameter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private void QAssayCountApply()
        {
            QAssayCountShow(_gate.CAssayRead());
            QAssayCountShow(3);
        }

        private void QAssayCountShow(int count)
        {
            _count = count;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 19, "QAssay._count", "Contesting",
            "written by the engine at line 24 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineParameter_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private void QAssayCountApply()
        {
            QAssayCountShow(_gate.CAssayRead());
            QAssayCountShow(_gate.CAssayRead());
        }

        private void QAssayCountShow(int count)
        {
            _count = count;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_GroupParameter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private void QAssayCountApply()
        {
            QAssayCountShow(_gate.CAssayRead());
            Array.ForEach(new[] { 3 }, QAssayCountShow);
        }

        private void QAssayCountShow(int count)
        {
            _count = count;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 19, "QAssay._count", "Contesting",
            "written by the engine at line 24 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_BlankWriter_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
            private string _text = "";
    """, """
                _text = gate.CAssayRead().ToString();
                _text = string.Empty;
                _text = "";
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_ZeroWriter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
            private int _count;
    """, """
                _count = gate.CAssayRead();
                _count = 0;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "QAssay._count", "Contesting",
            "written by the engine at line 12 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_FalseWriter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
            private bool _flag;
    """, """
                _flag = gate.CAssayRead() > 0;
                _flag = false;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "QAssay._flag", "Contesting",
            "written by the engine at line 12 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineKey_AllowsCapsuleRead()
    {
        Assert.Empty(TAssayCapsuleRun("""
                _count = _capsule.LAssayCapsuleRead(gate.CAssayRead());
                _count = 3;
    """));
    }

    [Fact]
    public void AuditTruth_CapsuleRead_ReportsContesting()
    {
        Assert.Equal(
            [
                new TViolation(
                    TAssayTruth.TAssayDriverPath, 14, "QAssay._count", "Contesting",
                    "written by the engine at line 13 and by the shell here")
            ],
            TAssayCapsuleRun("""
                _count = gate.CAssayRead();
                _count = _capsule.LAssayCapsuleRead(gate.CAssayRead());
    """));
    }

    private static List<TViolation> TAssayCapsuleRun(string body)
    {
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayTruth.TAssayDriverPath] = TAssayTruth.TAssayDriverFormat("""
            private int _count;
            private readonly Cadroue.UIDeportment.Capsule.Assay.LAssayCapsule _capsule = new();
    """, body),
            [TAssayTruth.TAssayGatePath] = TAssayTruth.TAssayGateText,
            [TAssayCapsulePath] = TAssayCapsuleText,
        };
        string driverPath = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayTruth.TAssayDriverPath));
        return TAuditBinder.TAuditAssayRun(sources, () => TAuditTruthWalker.TAuditRun([driverPath]))
            .Where(hit => hit.TViolationKind == "Contesting")
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
    }
}
