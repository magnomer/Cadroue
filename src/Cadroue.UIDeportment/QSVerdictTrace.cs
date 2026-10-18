using System.Text;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The encoder verification's trace record: how many encoders passed, with one detail line per row.
public static class QSVerdictTrace
{
    public static void QSVerdictRecord(string lKind, IReadOnlyList<QSVerdictRow> lRows)
    {
        int lPassed = lRows.Count(lRow => lRow.QSVerdictSuccess);
        string lDetail = QSVerdictDetailFormat(lRows);
        LTraceLog.LTraceInfoRecord(
            $"Encoder verification ({lKind}): {lPassed} of {lRows.Count} encoder(s) available",
            lDetail);
    }

    private static string QSVerdictDetailFormat(IReadOnlyList<QSVerdictRow> lRows)
    {
        var lDetail = new StringBuilder();
        foreach (QSVerdictRow lRow in lRows)
        {
            lDetail.AppendLine(
                $"{lRow.QSVerdictFamily} / {lRow.QSVerdictEncoder}: {(lRow.QSVerdictSuccess ? "OK" : "FAIL")} - "
                + $"{lRow.QSVerdictMessage}");
        }

        return lDetail.ToString().TrimEnd();
    }
}
