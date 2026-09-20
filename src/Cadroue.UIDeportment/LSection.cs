using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LSectionRow(
    int LSectionRowIndex,
    string LSectionRowNumber,
    string LSectionRowName,
    string LSectionRowText,
    bool LSectionRowUnnamed,
    string LSectionRowPrefix,
    string LSectionRowSuffix,
    bool LSectionRowPrefixed,
    bool LSectionRowSuffixed,
    IReadOnlyList<string> LSectionRowAffixes,
    string LSectionRowOrigin,
    string LSectionRowEnd,
    string LSectionRowSpan,
    int LSectionRowColor,
    bool LSectionRowHidden,
    bool LSectionRowSelected,
    bool LSectionRowEditing);

public sealed class LSection
{
    private const string LSectionReturnKey = "Return";
    private const string LSectionEscapeKey = "Escape";
    private const string LSectionStepText = ",";

    private readonly LFlow lFlow;
    private IReadOnlyList<LPiece> lSectionPieces;
    private HashSet<int> lSectionSelected;
    private int lSectionRowCount;
    private bool lSectionEditable;
    private bool lSectionMinimized;
    private int? lSectionEditIndex;
    private int lSectionEditSerial;
    private string lSectionEditName = string.Empty;
    private string lSectionEditPrefix = string.Empty;
    private string lSectionEditSuffix = string.Empty;

    public event Action? LSectionChange;
    public event Action? LSectionSelectApply;
    public event Action<bool>? LSectionEnabledApply;
    public event Action<bool>? LSectionMinimizeChange;

    public LSection(LFlow lOwner)
    {
        lFlow = lOwner;
        lSectionPieces = lFlow.LFlowSection.LFlowSectionsRead().ToArray();
        lSectionSelected = new HashSet<int>(lFlow.LFlowSection.LFlowSelectedRead());
        lSectionRowCount = lSectionPieces.Count;
        lSectionEditable = lFlow.LFlowSectionEditable;
        LSectionDrag = new LSectionDrag(this, lFlow);
        lFlow.LFlowSection.LFlowSectionChange += LSectionUpdateHandle;
        lFlow.LFlowEditChange += LSectionEditHandle;
    }

    public LSectionDrag LSectionDrag { get; }

    public LFlow LFlow => lFlow;

    public bool LSectionEditable => lSectionEditable;

    public int LSectionRowCount => lSectionRowCount;

    public bool LSectionMinimized => lSectionMinimized;

    public int? LSectionEditIndex => lSectionEditIndex;

    public int LSectionEditSerial => lSectionEditSerial;

    public static int LSectionCountRead() => LSectionPalette.LSectionActiveCount;

    public static string LSectionHexRead(int lColorIndex) => LSectionPalette.LSectionColorRead(lColorIndex);

    public static IReadOnlyList<string> LSectionPaletteRead(string lName) => LSectionPalette.LSectionColorsRead(lName);

    public static string LSectionNumberRead(int lIndex) => (lIndex + 1).ToString();

    public static bool LSectionStepCheck(string lText) =>
        string.Equals(lText, LSectionStepText, StringComparison.Ordinal);

    public bool LSectionSelectedCheck(int lIndex) => lSectionSelected.Contains(lIndex);

    public string LSectionTitleRead() =>
        lSectionPieces.Count == 0
            ? LLocalization.LLocalizationTextRead("Section.Header.Title")
            : LLocalization.LLocalizationFormat("Section.Header.Count", lSectionPieces.Count);

    public IReadOnlyList<LSectionRow> LSectionRowsRead() =>
        lSectionPieces.Select(LSectionRowCreate).ToArray();

    public bool LSectionMinimizedSet(bool lMinimized)
    {
        if (lSectionMinimized == lMinimized)
        {
            return false;
        }

        lSectionMinimized = lMinimized;
        LSectionMinimizeChange?.Invoke(lMinimized);
        return true;
    }

    public void LSectionToggleHandle(int lIndex)
    {
        LSectionDrag.LSectionDragClear();
        LSectionEditCommit();
        if (lSectionEditable)
        {
            lFlow.LFlowSection.LFlowSectionToggle(lIndex);
        }
    }

    public bool LSectionSeekHandle(int lIndex, int lClickCount, bool lEnd)
    {
        if (lClickCount < 2)
        {
            return false;
        }

        LSectionDrag.LSectionDragClear();
        if (lIndex < 0)
        {
            return false;
        }

        LSectionEditCommit();
        lFlow.LFlowSection.LFlowSectionSeek(lIndex, lEnd);
        return true;
    }

    public bool LSectionRenameHandle(int lIndex, int lClickCount)
    {
        if (lClickCount < 2 || !lSectionEditable)
        {
            return false;
        }

        LSectionDrag.LSectionDragClear();
        if (lIndex < 0)
        {
            return false;
        }

        lFlow.LFlowSection.LFlowSectionSelect(lIndex);
        LSectionEditStart(lIndex);
        return true;
    }

    public void LSectionEditStart(int lIndex)
    {
        LPiece lPiece = lSectionPieces[lIndex];
        lSectionEditIndex = lIndex;
        lSectionEditSerial++;
        lSectionEditName = lPiece.LPieceName;
        lSectionEditPrefix = lPiece.LPiecePrefix;
        lSectionEditSuffix = lPiece.LPieceSuffix;
        LSectionRebuildRaise();
    }

    public void LSectionTextSet(string lName, string lPrefix, string lSuffix)
    {
        lSectionEditName = lName;
        lSectionEditPrefix = lPrefix;
        lSectionEditSuffix = lSuffix;
    }

    public void LSectionEditCommit()
    {
        if (lSectionEditIndex is not int lIndex)
        {
            return;
        }

        lSectionEditIndex = null;
        lFlow.LFlowSection.LFlowNameSet(
            lIndex, lSectionEditName.Trim(), lSectionEditPrefix.Trim(), lSectionEditSuffix.Trim());
        LSectionRebuildRaise();
    }

    public void LSectionEditCancel()
    {
        lSectionEditIndex = null;
        LSectionRebuildRaise();
    }

    public bool LSectionKeyRun(string lKey)
    {
        if (string.Equals(lKey, LSectionReturnKey, StringComparison.Ordinal))
        {
            LSectionEditCommit();
            return true;
        }

        if (string.Equals(lKey, LSectionEscapeKey, StringComparison.Ordinal))
        {
            LSectionEditCancel();
            return true;
        }

        return false;
    }

    public void LSectionBlurHandle(int lSerial, bool lFocusInside)
    {
        if (lSerial != lSectionEditSerial || lFocusInside)
        {
            return;
        }

        LSectionEditCommit();
    }

    public void LSectionDeleteRun()
    {
        LSectionEditCommit();
        lFlow.LFlowSection.LFlowSectionDelete();
    }

    public void LSectionSortRun()
    {
        LSectionEditCommit();
        lFlow.LFlowSection.LFlowSectionSort();
    }

    public void LSectionClearRun()
    {
        LSectionEditCommit();
        lFlow.LFlowSection.LFlowSectionClear();
    }

    private void LSectionUpdateHandle(IReadOnlyList<LPiece> lPieces, int? lSelected)
    {
        LPiece[] lNext = lPieces.ToArray();
        bool lSame = lSectionPieces.SequenceEqual(lNext) && lSectionRowCount == lNext.Length;
        lSectionPieces = lNext;
        lSectionSelected = new HashSet<int>(lFlow.LFlowSection.LFlowSelectedRead());
        if (LSectionDrag.LSectionDragActive)
        {
            return;
        }

        if (lSame)
        {
            LSectionSelectApply?.Invoke();
            return;
        }

        LSectionRebuildRaise();
    }

    private void LSectionEditHandle(bool lEditable)
    {
        lSectionEditable = lEditable;
        LSectionEnabledApply?.Invoke(lEditable);
        if (lEditable)
        {
            return;
        }

        LSectionDrag.LSectionDragClear();
        if (lSectionEditIndex is not null)
        {
            LSectionEditCancel();
        }
    }

    public void LSectionRebuildRaise()
    {
        lSectionRowCount = lSectionPieces.Count;
        LSectionChange?.Invoke();
    }

    private LSectionRow LSectionRowCreate(LPiece lPiece, int lIndex)
    {
        bool lUnnamed = string.IsNullOrEmpty(lPiece.LPieceName);
        TimeSpan lSpan = lPiece.LPieceEnd - lPiece.LPieceOrigin;
        string[] lAffixes = new[] { lPiece.LPiecePrefix, lPiece.LPieceSuffix }
            .Where(lAffix => !string.IsNullOrEmpty(lAffix))
            .Select(lAffix => $"  /  {lAffix}")
            .ToArray();
        return new LSectionRow(
            lIndex,
            LSectionNumberRead(lIndex),
            lPiece.LPieceName,
            lUnnamed ? LLocalization.LLocalizationFormat("Section.DefaultName", lIndex + 1) : lPiece.LPieceName,
            lUnnamed,
            lPiece.LPiecePrefix,
            lPiece.LPieceSuffix,
            !string.IsNullOrEmpty(lPiece.LPiecePrefix),
            !string.IsNullOrEmpty(lPiece.LPieceSuffix),
            lAffixes,
            LSectionTimeFormat(lPiece.LPieceOrigin),
            LSectionTimeFormat(lPiece.LPieceEnd),
            $"  ({LSectionTimeFormat(lSpan < TimeSpan.Zero ? TimeSpan.Zero : lSpan)})",
            lPiece.LPieceColorIndex,
            lPiece.LPieceHidden,
            lSectionSelected.Contains(lIndex),
            lSectionEditIndex == lIndex);
    }

    private static string LSectionTimeFormat(TimeSpan lTime) =>
        lTime.TotalHours >= 1
            ? $"{(int)lTime.TotalHours}:{lTime.Minutes:D2}:{lTime.Seconds:D2}"
            : $"{lTime.Minutes}:{lTime.Seconds:D2}";
}
