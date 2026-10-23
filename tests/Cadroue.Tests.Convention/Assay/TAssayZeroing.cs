using Xunit;

namespace Convention.Tests;

public sealed class TAssayZeroing
{
    private const string TAssayRemoveText = """
            private static void QAssayRemove(CAssay gate, long? id)
            {
                gate.CAssayRemove(id);
            }
        """;

    private const string TAssayItemText = """

        public sealed class QAssayItem
        {
            public QAssayItem(long? id)
            {
                QAssayItemId = id;
            }

            public long? QAssayItemId { get; }
        }
        """;

    [Fact]
    public void AuditTruth_ZeroForward_ReportsZeroing()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat(TAssayRemoveText, """
                    QAssayRemove(gate, 0);
            """), "Zeroing", new TViolation(
            TAssayTruth.TAssayDriverPath, 15, "QAssayRemove", "Zeroing", "passes 0 to QAssayRemove"));
    }

    [Fact]
    public void AuditTruth_NullForward_AllowsNull()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat(TAssayRemoveText, """
                    QAssayRemove(gate, null);
            """), "Zeroing", null);
    }

    [Fact]
    public void AuditTruth_ZeroConstructor_ReportsZeroing()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    QAssayItem item = new QAssayItem(0);
                    gate.CAssayRemove(item.QAssayItemId);
            """) + TAssayItemText, "Zeroing", new TViolation(
            TAssayTruth.TAssayDriverPath, 10, "QAssayItem", "Zeroing", "passes 0 to new QAssayItem"));
    }

    [Fact]
    public void AuditTruth_NullConstructor_AllowsNull()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    QAssayItem item = new QAssayItem(null);
                    gate.CAssayRemove(item.QAssayItemId);
            """) + TAssayItemText, "Zeroing", null);
    }

    [Fact]
    public void AuditTruth_ZeroCompare_ReportsZeroing()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    QAssayItem item = new QAssayItem(null);
                    if (item.QAssayItemId == 0)
                    {
                        return;
                    }

                    gate.CAssayRemove(item.QAssayItemId);
            """) + TAssayItemText, "Zeroing", new TViolation(
            TAssayTruth.TAssayDriverPath, 11, "QAssayItemId", "Zeroing", "compares QAssayItemId with 0"));
    }

    [Fact]
    public void AuditTruth_NullCompare_AllowsNull()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                    QAssayItem item = new QAssayItem(null);
                    if (item.QAssayItemId == null)
                    {
                        return;
                    }

                    gate.CAssayRemove(item.QAssayItemId);
            """) + TAssayItemText, "Zeroing", null);
    }

    [Fact]
    public void AuditTruth_ZeroHelper_AllowsZero()
    {
        TAssayTruth.TAssayTruthCheck(TAssayTruth.TAssayDriverFormat("""
                private static void QAssayWidthRefine(int width)
                {
                    Console.WriteLine(width);
                }
            """, """
                    QAssayWidthRefine(0);
                    gate.CAssayRemove(null);
            """), "Zeroing", null);
    }
}
