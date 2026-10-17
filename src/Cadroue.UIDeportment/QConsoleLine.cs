using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QConsoleSpan(string QConsoleRunText, bool QConsoleRunAccent);

public sealed record QConsoleStatus(
    IReadOnlyList<QConsoleSpan> QConsoleStatusRuns,
    bool QConsoleStatusRunning,
    bool QConsoleStatusLoaded,
    bool QConsoleStatusBusy,
    bool QConsoleStatusActive,
    bool QConsoleStatusRemovable,
    bool QConsoleStatusDone,
    bool QConsoleStatusClearable,
    bool QConsoleStatusBoard,
    bool QConsoleStatusAuto,
    string QConsoleStatusStation);

// The console's wording: reads the gate's texts through the catalog and splits the status line around its accent.
public static class QConsoleLine
{
    internal static QConsoleStatus QConsoleStatusCreate(CConsoleStatus lStatus) =>
        new(
            QConsoleRunsResolve(
                QConsoleTextRead(lStatus.CConsoleStatusLine),
                lStatus.CConsoleStatusAccent is { } lAccent ? QConsoleTextRead(lAccent) : null),
            lStatus.CConsoleStatusRunning,
            lStatus.CConsoleStatusLoaded,
            lStatus.CConsoleStatusBusy,
            lStatus.CConsoleStatusActive,
            lStatus.CConsoleStatusRemovable,
            lStatus.CConsoleStatusDone,
            lStatus.CConsoleStatusClearable,
            lStatus.CConsoleStatusBoard,
            lStatus.CConsoleStatusAuto,
            QConsoleArgumentRead(lStatus.CConsoleStatusStation).ToString() ?? string.Empty);

    // A text without arguments is read as plain text; with them it is formatted, each nested text worded first.
    public static string QConsoleTextRead(CConsoleText lText) =>
        lText.CConsoleTextArguments is { } lArguments
            ? QLocalizationCatalog.QLocalizationFormat(
                lText.CConsoleTextKey, lArguments.Select(QConsoleArgumentRead).ToArray())
            : QLocalizationCatalog.QLocalizationTextRead(lText.CConsoleTextKey);

    private static object QConsoleArgumentRead(object lArgument) =>
        lArgument is CConsoleText lText ? QConsoleTextRead(lText) : lArgument;

    public static IReadOnlyList<QConsoleSpan> QConsoleRunsResolve(string lLine, string? lAccent)
    {
        int lAccentIndex = string.IsNullOrEmpty(lAccent) ? -1 : lLine.IndexOf(lAccent, StringComparison.Ordinal);
        if (lAccentIndex < 0)
        {
            return [new QConsoleSpan(lLine, false)];
        }

        var lRuns = new List<QConsoleSpan>();
        if (lAccentIndex > 0)
        {
            lRuns.Add(new QConsoleSpan(lLine[..lAccentIndex], false));
        }

        lRuns.Add(new QConsoleSpan(lAccent!, true));
        int lAccentEnd = lAccentIndex + lAccent!.Length;
        if (lAccentEnd < lLine.Length)
        {
            lRuns.Add(new QConsoleSpan(lLine[lAccentEnd..], false));
        }

        return lRuns;
    }
}
