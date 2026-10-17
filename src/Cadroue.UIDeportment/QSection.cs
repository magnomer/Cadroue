using Cadroue.Core;
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

public sealed class QSection
{
    private const string QSectionReturnKey = "Return";
    private const string QSectionEscapeKey = "Escape";
    private const string QSectionStepText = ",";

    private readonly QFlow qFlow;
    private IReadOnlyList<LPiece> qSectionPieces;
    private HashSet<int> qSectionSelected;
    private int qSectionRowCount;
    private bool qSectionMinimized;
    private int? qSectionEditIndex;
    private int qSectionEditSerial;
    private string qSectionEditName = string.Empty;
    private string qSectionEditPrefix = string.Empty;
    private string qSectionEditSuffix = string.Empty;

    public event Action? QSectionChanged;
    public event Action? QSectionSelectApplied;
    public event Action<bool>? QSectionEnabledApplied;
    public event Action<bool>? QSectionMinimizeChanged;

    public QSection(QFlow lOwner)
    {
        qFlow = lOwner;
        qSectionPieces = qFlow.QFlowSection.QFlowSectionsRead().ToArray();
        qSectionSelected = new HashSet<int>(qFlow.QFlowSection.QFlowSelectedRead());
        qSectionRowCount = qSectionPieces.Count;
        QSectionDrag = new QSectionDrag(this, qFlow);
        QSectionFlowAttach();
    }

    public QSectionDrag QSectionDrag { get; }

    public QFlow QFlow => qFlow;

    public bool QSectionEditable => qFlow.QFlowEdit.QFlowSectionEditable;

    public int QSectionRowCount => qSectionRowCount;

    public bool QSectionMinimized => qSectionMinimized;

    public int? QSectionEditIndex => qSectionEditIndex;

    public int QSectionEditSerial => qSectionEditSerial;

    public static int QSectionCountRead() => LSectionPalette.LSectionActiveCount;

    public static string QSectionHexRead(int lColorIndex) => LSectionPalette.LSectionColorRead(lColorIndex);

    public static IReadOnlyList<string> QSectionPaletteRead(string lName) => LSectionPalette.LSectionColorsRead(lName);

    public static string QSectionNumberRead(int lIndex) => (lIndex + 1).ToString();

    public static bool QSectionStepCheck(string lText) =>
        string.Equals(lText, QSectionStepText, StringComparison.Ordinal);

    public bool QSectionSelectedCheck(int lIndex) => qSectionSelected.Contains(lIndex);

    public string QSectionTitleRead() =>
        qSectionPieces.Count == 0
            ? QLocalizationCatalog.QLocalizationTextRead("Section.Header.Title")
            : QLocalizationCatalog.QLocalizationFormat("Section.Header.Count", qSectionPieces.Count);

    public IReadOnlyList<QSectionRow> QSectionRowsRead() =>
        qSectionPieces.Select(QSectionRowCreate).ToArray();

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

    public void QSectionToggleHandle(int lIndex)
    {
        QSectionDrag.QSectionDragClear();
        QSectionEditCommit();
        if (QSectionEditable)
        {
            qFlow.QFlowSection.QFlowSectionToggle(lIndex);
        }
    }

    public bool QSectionSeekHandle(int lIndex, int lClickCount, bool lEnd)
    {
        if (lClickCount < 2)
        {
            return false;
        }

        QSectionDrag.QSectionDragClear();
        if (lIndex < 0)
        {
            return false;
        }

        QSectionEditCommit();
        qFlow.QFlowSection.QFlowSectionSeek(lIndex, lEnd);
        return true;
    }

    public bool QSectionRenameHandle(int lIndex, int lClickCount)
    {
        if (lClickCount < 2 || !QSectionEditable)
        {
            return false;
        }

        QSectionDrag.QSectionDragClear();
        if (lIndex < 0)
        {
            return false;
        }

        qFlow.QFlowSection.QFlowSectionSelect(lIndex);
        QSectionEditStart(lIndex);
        return true;
    }

    public void QSectionEditStart(int lIndex)
    {
        LPiece lPiece = qSectionPieces[lIndex];
        qSectionEditIndex = lIndex;
        qSectionEditSerial++;
        qSectionEditName = lPiece.LPieceName;
        qSectionEditPrefix = lPiece.LPiecePrefix;
        qSectionEditSuffix = lPiece.LPieceSuffix;
        QSectionRebuildRaise();
    }

    public void QSectionTextSet(string lName, string lPrefix, string lSuffix)
    {
        qSectionEditName = lName;
        qSectionEditPrefix = lPrefix;
        qSectionEditSuffix = lSuffix;
    }

    public void QSectionEditCommit()
    {
        if (QSectionEditRelease() is not int lIndex)
        {
            return;
        }

        qFlow.QFlowSection.QFlowNameSet(
            lIndex, qSectionEditName.Trim(), qSectionEditPrefix.Trim(), qSectionEditSuffix.Trim());
        QSectionRebuildRaise();
    }

    public void QSectionEditCancel()
    {
        qSectionEditIndex = null;
        QSectionRebuildRaise();
    }

    public bool QSectionKeyRun(string lKey)
    {
        if (string.Equals(lKey, QSectionReturnKey, StringComparison.Ordinal))
        {
            QSectionEditCommit();
            return true;
        }

        if (string.Equals(lKey, QSectionEscapeKey, StringComparison.Ordinal))
        {
            QSectionEditCancel();
            return true;
        }

        return false;
    }

    public void QSectionBlurHandle(int lSerial, bool lFocusInside)
    {
        if (lSerial != qSectionEditSerial || lFocusInside)
        {
            return;
        }

        QSectionEditCommit();
    }

    public void QSectionDeleteRun()
    {
        QSectionEditCommit();
        qFlow.QFlowSection.QFlowSectionDelete();
    }

    public void QSectionSortRun()
    {
        QSectionEditCommit();
        qFlow.QFlowSection.QFlowSectionSort();
    }

    public void QSectionClearRun()
    {
        QSectionEditCommit();
        qFlow.QFlowSection.QFlowSectionClear();
    }

    private void QSectionFlowAttach()
    {
        qFlow.QFlowSection.QFlowSectionChanged += QSectionUpdateHandle;
        qFlow.QFlowEdit.QFlowEditChanged += QSectionEditHandle;
    }

    private int? QSectionEditRelease()
    {
        int? lIndex = qSectionEditIndex;
        qSectionEditIndex = null;
        return lIndex;
    }

    private bool QSectionSameCheck(LPiece[] lNext) =>
        qSectionPieces.SequenceEqual(lNext) && qSectionRowCount == lNext.Length;

    private void QSectionPieceSet(LPiece[] lNext)
    {
        qSectionPieces = lNext;
        qSectionSelected = new HashSet<int>(qFlow.QFlowSection.QFlowSelectedRead());
    }

    private void QSectionUpdateHandle(IReadOnlyList<LPiece> lPieces, int? lSelected)
    {
        LPiece[] lNext = lPieces.ToArray();
        bool lSame = QSectionSameCheck(lNext);
        QSectionPieceSet(lNext);
        if (QSectionDrag.QSectionDragActive)
        {
            return;
        }

        if (lSame)
        {
            QSectionSelectApplied?.Invoke();
            return;
        }

        QSectionRebuildRaise();
    }

    private void QSectionEditHandle(bool lEditable)
    {
        QSectionEnabledApplied?.Invoke(lEditable);
        if (lEditable)
        {
            return;
        }

        QSectionDrag.QSectionDragClear();
        if (qSectionEditIndex is not null)
        {
            QSectionEditCancel();
        }
    }

    public void QSectionRebuildRaise()
    {
        qSectionRowCount = qSectionPieces.Count;
        QSectionChanged?.Invoke();
    }

    private QSectionRow QSectionRowCreate(LPiece lPiece, int lIndex)
    {
        bool lUnnamed = string.IsNullOrEmpty(lPiece.LPieceName);
        TimeSpan lSpan = lPiece.LPieceEnd - lPiece.LPieceOrigin;
        string[] lAffixes = new[] { lPiece.LPiecePrefix, lPiece.LPieceSuffix }
            .Where(lAffix => !string.IsNullOrEmpty(lAffix))
            .Select(lAffix => $"  /  {lAffix}")
            .ToArray();
        return new QSectionRow(
            lIndex,
            QSectionNumberRead(lIndex),
            lPiece.LPieceName,
            lUnnamed ? QLocalizationCatalog.QLocalizationFormat("Section.DefaultName", lIndex + 1) : lPiece.LPieceName,
            lUnnamed,
            lPiece.LPiecePrefix,
            lPiece.LPieceSuffix,
            !string.IsNullOrEmpty(lPiece.LPiecePrefix),
            !string.IsNullOrEmpty(lPiece.LPieceSuffix),
            lAffixes,
            QSectionTimeFormat(lPiece.LPieceOrigin),
            QSectionTimeFormat(lPiece.LPieceEnd),
            $"  ({QSectionTimeFormat(lSpan < TimeSpan.Zero ? TimeSpan.Zero : lSpan)})",
            lPiece.LPieceColorIndex,
            lPiece.LPieceHidden,
            qSectionSelected.Contains(lIndex),
            qSectionEditIndex == lIndex);
    }

    private static string QSectionTimeFormat(TimeSpan lTime) =>
        lTime.TotalHours >= 1
            ? $"{(int)lTime.TotalHours}:{lTime.Minutes:D2}:{lTime.Seconds:D2}"
            : $"{lTime.Minutes}:{lTime.Seconds:D2}";
}
