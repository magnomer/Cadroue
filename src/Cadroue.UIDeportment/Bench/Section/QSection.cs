using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The sections panel's wiring: reads the section gate's rows, hands each row input to the gate with the editable
// switch, and relays the gate's tells. Only the panel's minimized state is its own.
public sealed class QSection
{
    private const string QSectionStepText = ",";

    private readonly QFlow qFlow;
    private readonly CSectionList qSectionGate;
    private bool qSectionMinimized;

    public event Action? QSectionChanged;
    public event Action? QSectionSelectApplied;
    public event Action<bool>? QSectionEnabledApplied;
    public event Action<bool>? QSectionMinimizeChanged;

    public QSection(QFlow lOwner)
    {
        qFlow = lOwner;
        qSectionGate = qFlow.QFlowSection.QFlowSectionGate.CSectionList;
        QSectionDrag = new QSectionDrag(qSectionGate.CSectionDrag);
        QSectionEdit = new QSectionEdit(qSectionGate.CSectionEdit);
        QSectionFlowAttach();
    }

    public QSectionDrag QSectionDrag { get; }

    public QSectionEdit QSectionEdit { get; }

    public QFlow QFlow => qFlow;

    public bool QSectionEditable => qFlow.QFlowEdit.QFlowSectionEditable;

    public bool QSectionMinimized => qSectionMinimized;

    public static int QSectionCountRead() => LSectionPalette.LSectionActiveCount;

    public static string QSectionHexRead(int lColorIndex) => LSectionPalette.LSectionColorRead(lColorIndex);

    public static IReadOnlyList<string> QSectionPaletteRead(string lName) => LSectionPalette.LSectionColorsRead(lName);

    public static bool QSectionStepCheck(string lText) =>
        string.Equals(lText, QSectionStepText, StringComparison.Ordinal);

    // Forwarded for the panel surface's selection repaint.
    public bool QSectionSelectedCheck(int lIndex) => qSectionGate.CSectionSelectedCheck(lIndex);

    public string QSectionTitleRead() =>
        qSectionGate.CSectionHeldCount is int lCount
            ? QLocalizationCatalog.QLocalizationFormat("Section.Header.Count", lCount)
            : QLocalizationCatalog.QLocalizationTextRead("Section.Header.Title");

    public IReadOnlyList<QSectionRow> QSectionRowsRead() =>
        qSectionGate.CSectionRowsRead().Select(QSectionRow.QSectionRowCreate).ToArray();

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
        qFlow.QFlowSection.QFlowSectionGate.CSectionChanged += QSectionUpdateHandle;
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
}
