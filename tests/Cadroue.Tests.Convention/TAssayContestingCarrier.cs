using Xunit;

namespace Convention.Tests;

public sealed class TAssayContestingCarrier
{
    [Fact]
    public void AuditTruth_PlainRecord_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private sealed record QAssayRow(int QAssayRowCount);

        private void QAssayRowTake()
        {
            Array.ForEach(new[] { new QAssayRow(3) }, QAssayRowShow);
        }

        private void QAssayRowShow(QAssayRow row)
        {
            _count = row.QAssayRowCount;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 20, "QAssay._count", "Contesting",
            "written by the engine at line 25 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineRecord_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private sealed record QAssayRow(int QAssayRowCount);

        private void QAssayRowTake()
        {
            Array.ForEach(new[] { new QAssayRow(_gate.CAssayRead()) }, QAssayRowShow);
        }

        private void QAssayRowShow(QAssayRow row)
        {
            _count = row.QAssayRowCount;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_PlainProperty_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private sealed class QAssayBox
        {
            public int QAssayBoxCount { get; set; }
        }

        private void QAssayBoxTake(QAssayBox box)
        {
            box.QAssayBoxCount = 3;
            _count = box.QAssayBoxCount;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 19, "QAssay._count", "Contesting",
            "written by the engine at line 24 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineProperty_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private sealed class QAssayBox
        {
            public int QAssayBoxCount { get; set; }
        }

        private void QAssayBoxTake(QAssayBox box)
        {
            box.QAssayBoxCount = _gate.CAssayRead();
            _count = box.QAssayBoxCount;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_PlainConstructor_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private sealed class QAssayCell
        {
            public QAssayCell(int count)
            {
                QAssayCellCount = count;
            }

            public int QAssayCellCount { get; }
        }

        private void QAssayCellTake()
        {
            Array.ForEach(new[] { new QAssayCell(3) }, QAssayCellShow);
        }

        private void QAssayCellShow(QAssayCell cell)
        {
            _count = cell.QAssayCellCount;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 28, "QAssay._count", "Contesting",
            "written by the engine at line 33 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineConstructor_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private readonly CAssay _gate = new();

        private sealed class QAssayCell
        {
            public QAssayCell(int count)
            {
                QAssayCellCount = count;
            }

            public int QAssayCellCount { get; }
        }

        private void QAssayCellTake()
        {
            Array.ForEach(new[] { new QAssayCell(_gate.CAssayRead()) }, QAssayCellShow);
        }

        private void QAssayCellShow(QAssayCell cell)
        {
            _count = cell.QAssayCellCount;
        }
    """, """
            _count = gate.CAssayRead();
    """), "Contesting", null);
    }
}
