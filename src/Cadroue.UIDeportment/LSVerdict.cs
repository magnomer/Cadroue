using System.Text;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LSVerdictRow(
    string LSVerdictFamily,
    string LSVerdictEncoder,
    bool LSVerdictSuccess,
    string LSVerdictMessage);

public sealed class LSVerdict
{
    private readonly string lsVerdictTitle;
    private readonly IReadOnlyList<LSVerdictRow> lsVerdictRows;

    public LSVerdict(string lTitle, IReadOnlyList<LSVerdictRow> lRows)
    {
        lsVerdictTitle = lTitle;
        lsVerdictRows = lRows;
    }

    public string LSVerdictTitle => lsVerdictTitle;

    public IReadOnlyList<LSVerdictRow> LSVerdictRows => lsVerdictRows;

    public static bool LSVerdictDetailCheck(LSVerdictRow lRow)
    {
        string lMessage = lRow.LSVerdictMessage.Trim();
        if (lMessage.Length == 0)
        {
            return false;
        }

        return !(lRow.LSVerdictSuccess && string.Equals(lMessage, "exit 0", StringComparison.OrdinalIgnoreCase));
    }

    public static void LSVerdictRecord(string lKind, IReadOnlyList<LSVerdictRow> lRows)
    {
        int lPassed = lRows.Count(lRow => lRow.LSVerdictSuccess);
        var lDetail = new StringBuilder();
        foreach (LSVerdictRow lRow in lRows)
        {
            lDetail.AppendLine(
                $"{lRow.LSVerdictFamily} / {lRow.LSVerdictEncoder}: {(lRow.LSVerdictSuccess ? "OK" : "FAIL")} - "
                + $"{lRow.LSVerdictMessage}");
        }

        LTraceLog.LTraceInfoRecord(
            $"Encoder verification ({lKind}): {lPassed} of {lRows.Count} encoder(s) available",
            lDetail.ToString().TrimEnd());
    }
}
