using System.Text;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QSVerdictRow(
    string QSVerdictFamily,
    string QSVerdictEncoder,
    bool QSVerdictSuccess,
    string QSVerdictMessage);

public sealed class QSVerdict
{
    private readonly string qsVerdictTitle;
    private readonly IReadOnlyList<QSVerdictRow> qsVerdictRows;

    public QSVerdict(string lTitle, IReadOnlyList<QSVerdictRow> lRows)
    {
        qsVerdictTitle = lTitle;
        qsVerdictRows = lRows;
    }

    public string QSVerdictTitle => qsVerdictTitle;

    public IReadOnlyList<QSVerdictRow> QSVerdictRows => qsVerdictRows;

    public static bool QSVerdictDetailCheck(QSVerdictRow lRow)
    {
        string lMessage = lRow.QSVerdictMessage.Trim();
        if (lMessage.Length == 0)
        {
            return false;
        }

        return !(lRow.QSVerdictSuccess && string.Equals(lMessage, "exit 0", StringComparison.OrdinalIgnoreCase));
    }

    public static void QSVerdictRecord(string lKind, IReadOnlyList<QSVerdictRow> lRows)
    {
        int lPassed = lRows.Count(lRow => lRow.QSVerdictSuccess);
        var lDetail = new StringBuilder();
        foreach (QSVerdictRow lRow in lRows)
        {
            lDetail.AppendLine(
                $"{lRow.QSVerdictFamily} / {lRow.QSVerdictEncoder}: {(lRow.QSVerdictSuccess ? "OK" : "FAIL")} - "
                + $"{lRow.QSVerdictMessage}");
        }

        LTraceLog.LTraceInfoRecord(
            $"Encoder verification ({lKind}): {lPassed} of {lRows.Count} encoder(s) available",
            lDetail.ToString().TrimEnd());
    }
}
