using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QSectionRow(
    int QSectionRowIndex,
    string QSectionRowNumber,
    string QSectionRowName,
    string QSectionRowText,
    bool QSectionRowUnnamed,
    string QSectionRowPrefix,
    string QSectionRowSuffix,
    bool QSectionRowPrefixed,
    bool QSectionRowSuffixed,
    IReadOnlyList<string> QSectionRowAffixes,
    string QSectionRowOrigin,
    string QSectionRowEnd,
    string QSectionRowSpan,
    int QSectionRowColor,
    bool QSectionRowHidden,
    bool QSectionRowSelected,
    bool QSectionRowEditing);

// The sections panel's wiring: paints the section gate's rows with their wording and times, hands each row input to
// the gate with the editable switch, and relays the gate's tells. Only the panel's minimized state is its own.
public sealed class QSection
{
    private const string QSectionReturnKey = "Return";
    private const string QSectionEscapeKey = "Escape";
    private const string QSectionStepText = ",";

    private readonly QFlow qFlow;
    private readonly CSection qSectionGate;
    private bool qSectionMinimized;

    public event Action? QSectionChanged;
    public event Action? QSectionSelectApplied;
    public event Action<bool>? QSectionEnabledApplied;
    public event Action<bool>? QSectionMinimizeChanged;

    public QSection(QFlow lOwner)
    {
        qFlow = lOwner;
        qSectionGate = qFlow.QFlowSection.QFlowSectionGate;
        QSectionDrag = new QSectionDrag(qSectionGate.CSectionDrag);
        QSectionFlowAttach();
    }

    public QSectionDrag QSectionDrag { get; }

    public QFlow QFlow => qFlow;

    public bool QSectionEditable => qFlow.QFlowEdit.QFlowSectionEditable;

    public bool QSectionMinimized => qSectionMinimized;

    // Forwarded for the row surface's editor.
    public int QSectionEditSerial => qSectionGate.CSectionEdit.CSectionEditSerial;

    public static int QSectionCountRead() => LSectionPalette.LSectionActiveCount;

    public static string QSectionHexRead(int lColorIndex) => LSectionPalette.LSectionColorRead(lColorIndex);

    public static IReadOnlyList<string> QSectionPaletteRead(string lName) => LSectionPalette.LSectionColorsRead(lName);

    public static string QSectionNumberRead(int lIndex) => (lIndex + 1).ToString();

    public static bool QSectionStepCheck(string lText) =>
        string.Equals(lText, QSectionStepText, StringComparison.Ordinal);

    // Forwarded for the panel surface's selection repaint.
    public bool QSectionSelectedCheck(int lIndex) => qSectionGate.CSectionSelectedCheck(lIndex);

    public string QSectionTitleRead() =>
        qSectionGate.CSectionHeldCount is int lCount
            ? QLocalizationCatalog.QLocalizationFormat("Section.Header.Count", lCount)
            : QLocalizationCatalog.QLocalizationTextRead("Section.Header.Title");

    public IReadOnlyList<QSectionRow> QSectionRowsRead() =>
        qSectionGate.CSectionRowsRead().Select(QSectionRowCreate).ToArray();

    public bool QSectionMinimizedSet(bool lMinimized)
    {
        if (qSectionMinimized == lMinimized)
        {
            return false;
        }

        qSectionMinimized = lMinimized;
        QSectionMinimizeChanged?.Invoke(lMinimized);
        return true;
    }

    public void QSectionToggleHandle(int lIndex) => qSectionGate.CSectionToggleHandle(lIndex);

    public bool QSectionSeekHandle(int lIndex, int lClickCount, bool lEnd) =>
        qSectionGate.CSectionSeekHandle(lIndex, lClickCount, lEnd);

    public bool QSectionRenameHandle(int lIndex, int lClickCount) =>
        qSectionGate.CSectionRenameHandle(lIndex, lClickCount);

    public void QSectionTextSet(string lName, string lPrefix, string lSuffix) =>
        qSectionGate.CSectionEdit.CSectionEditSet(lName, lPrefix, lSuffix);

    // The editor's keys: Return commits, Escape drops; any other key is not the editor's.
    public bool QSectionKeyRun(string lKey)
    {
        if (string.Equals(lKey, QSectionReturnKey, StringComparison.Ordinal))
        {
            qSectionGate.CSectionEdit.CSectionEditCommit();
            return true;
        }

        if (string.Equals(lKey, QSectionEscapeKey, StringComparison.Ordinal))
        {
            qSectionGate.CSectionEdit.CSectionEditCancel();
            return true;
        }

        return false;
    }

    public void QSectionBlurHandle(int lSerial, bool lFocusInside) =>
        qSectionGate.CSectionEdit.CSectionEditHandle(lSerial, lFocusInside);

    // The delete asks through QAskNotice, so it waits for the ask port; the pending edit is committed first.
    public void QSectionDeleteRun()
    {
        qSectionGate.CSectionEdit.CSectionEditCommit();
        qFlow.QFlowSection.QFlowSectionDelete();
    }

    public void QSectionSortRun() => qSectionGate.CSectionSortRun();

    // The clear asks through QAskNotice, so it waits for the ask port; the pending edit is committed first.
    public void QSectionClearRun()
    {
        qSectionGate.CSectionEdit.CSectionEditCommit();
        qFlow.QFlowSection.QFlowSectionClear();
    }

    private void QSectionFlowAttach()
    {
        qSectionGate.CSectionChanged += QSectionUpdateHandle;
        qFlow.QFlowEdit.QFlowEditChanged += QSectionEditHandle;
        qSectionGate.CSectionRowsChanged += QSectionChangeRaise;
        qSectionGate.CSectionSelectApplied += QSectionSelectRaise;
        qSectionGate.CSectionRowsSync();
    }

    private void QSectionUpdateHandle() => qSectionGate.CSectionRowsUpdate();

    private void QSectionEditHandle(bool lEditable)
    {
        QSectionEnabledApplied?.Invoke(lEditable);
        qSectionGate.CSectionEditableHandle(lEditable);
    }

    private void QSectionChangeRaise() => QSectionChanged?.Invoke();

    private void QSectionSelectRaise() => QSectionSelectApplied?.Invoke();

    private static QSectionRow QSectionRowCreate(CSectionRow lRow) =>
        new(
            lRow.CSectionRowIndex,
            QSectionNumberRead(lRow.CSectionRowIndex),
            lRow.CSectionRowName,
            lRow.CSectionRowUnnamed
                ? QLocalizationCatalog.QLocalizationFormat("Section.DefaultName", lRow.CSectionRowIndex + 1)
                : lRow.CSectionRowName,
            lRow.CSectionRowUnnamed,
            lRow.CSectionRowPrefix,
            lRow.CSectionRowSuffix,
            lRow.CSectionRowPrefixed,
            lRow.CSectionRowSuffixed,
            lRow.CSectionRowAffixes.Select(lAffix => $"  /  {lAffix}").ToArray(),
            QSectionTimeFormat(lRow.CSectionRowOrigin),
            QSectionTimeFormat(lRow.CSectionRowEnd),
            $"  ({QSectionTimeFormat(lRow.CSectionRowSpan)})",
            lRow.CSectionRowColor,
            lRow.CSectionRowHidden,
            lRow.CSectionRowSelected,
            lRow.CSectionRowEditing);

    private static string QSectionTimeFormat(TimeSpan lTime) =>
        lTime.TotalHours >= 1
            ? $"{(int)lTime.TotalHours}:{lTime.Minutes:D2}:{lTime.Seconds:D2}"
            : $"{lTime.Minutes}:{lTime.Seconds:D2}";
}
