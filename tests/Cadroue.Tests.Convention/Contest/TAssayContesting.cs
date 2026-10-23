using Xunit;

namespace Convention.Tests;

public sealed class TAssayContesting
{
    [Fact]
    public void AuditTruth_OnePlainWriter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
    """, """
                    _count = gate.CAssayRead();
                    _count = 3;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "QAssay._count", "Contesting",
            "written by the engine at line 12 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_TwoPlainWriters_ListsSecondWriter()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private int _count;
    """, """
                    _count = gate.CAssayRead();
                    _count = 3;
                    _count = 4;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "QAssay._count", "Contesting",
            "written by the engine at line 12 and by the shell here, also at QAssay.cs:14"));
    }

    [Fact]
    public void AuditTruth_PlainHeld_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
    """, """
            int held = items.Length;
            _count = gate.CAssayRead();
            _count = held;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 14, "QAssay._count", "Contesting",
            "written by the engine at line 13 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineHeld_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
    """, """
            int held = gate.CAssayRead();
            _count = gate.CAssayRead();
            _count = held;
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_PlainSetter_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;

        private int QAssayCount
        {
            get => _count;
            set => _count = value;
        }
    """, """
            _count = gate.CAssayRead();
            QAssayCount = items.Length;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 13, "QAssay._count", "Contesting",
            "written by the engine at line 18 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineSetter_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;

        private int QAssayCount
        {
            get => _count;
            set => _count = value;
        }
    """, """
            _count = gate.CAssayRead();
            QAssayCount = gate.CAssayRead();
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_ContentLookup_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;

        private static int QAssayLookupRead(int key) => key;
    """, """
            _count = QAssayLookupRead(gate.CAssayRead());
            _count = QAssayLookupRead(items.Length);
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "QAssay._count", "Contesting",
            "written by the engine at line 14 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_KeyLookup_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;

        private static int QAssayLookupRead(int key) => key;
    """, """
            _count = QAssayLookupRead(gate.CAssayRead());
            _count = QAssayLookupRead(3);
    """), "Contesting", null);
    }


    [Fact]
    public void AuditTruth_PlainHelper_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;

        private static void QAssayValueRefine(ref int field, int value)
        {
            field = value;
        }
    """, """
            _count = gate.CAssayRead();
            QAssayValueRefine(ref _count, items.Length);
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 18, "QAssay._count", "Contesting",
            "written by the engine at line 17 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_EngineHelper_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;

        private static void QAssayValueRefine(ref int field, int value)
        {
            field = value;
        }
    """, """
            _count = gate.CAssayRead();
            QAssayValueRefine(ref _count, gate.CAssayRead());
    """), "Contesting", null);
    }

    [Fact]
    public void AuditTruth_CopyOnly_ReportsContesting()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private int _mirror;

        private void QAssayMirrorSync(QAssay fresh)
        {
            _mirror = fresh._mirror;
        }
    """, """
            _count = gate.CAssayRead();
            _count = _mirror;
    """), "Contesting", new TViolation(
            TAssayTruth.TAssayDriverPath, 19, "QAssay._count", "Contesting",
            "written by the engine at line 18 and by the shell here"));
    }

    [Fact]
    public void AuditTruth_CopyBeside_AllowsEngineCopy()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
        private int _count;
        private int _mirror;

        private void QAssayMirrorSync(QAssay fresh)
        {
            _mirror = fresh._mirror;
        }
    """, """
            _mirror = gate.CAssayRead();
            _count = gate.CAssayRead();
            _count = _mirror;
    """), "Contesting", null);
    }
}
