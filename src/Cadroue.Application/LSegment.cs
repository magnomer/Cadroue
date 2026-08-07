using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LSegment
{
    private readonly List<LPiece> lSegmentPieces = new();
    private int? lSegmentIndexActive;
    private string? lSegmentSourcePath;

    public Action<IReadOnlyList<LPiece>, int?>? LSegmentChange;

    public IReadOnlyList<LPiece> LSegmentListRead() => lSegmentPieces.ToArray();

    public int? LSegmentSelectionRead() => lSegmentIndexActive;

    public string? LSegmentSourceRead() => lSegmentSourcePath;

    public void LSegmentSourceSet(string? lSegmentSource) => lSegmentSourcePath = lSegmentSource;

    public void LSegmentReset()
    {
        lSegmentPieces.Clear();
        lSegmentIndexActive = null;
    }

    public void LSegmentSet(IReadOnlyList<LPiece> lSegmentSections, int? lSegmentSelect)
    {
        List<LPiece> lSegmentList = lSegmentSections.ToList();
        int? lSegmentClamp = lSegmentList.Count == 0 || lSegmentSelect is not int lSelect
            ? null
            : Math.Clamp(lSelect, 0, lSegmentList.Count - 1);
        LSegmentApply(lSegmentList, lSegmentClamp);
    }

    public void LSegmentAdd(TimeSpan lSegmentCursor, TimeSpan lSegmentDuration, int lSegmentColorIndex, bool lSegmentOverlapAllowed)
    {
        if (LPiece.LPieceAdd(lSegmentPieces, lSegmentCursor, lSegmentDuration, lSegmentColorIndex, lSegmentOverlapAllowed)
            is not { } lSegmentPlan) return;
        LSegmentApply(lSegmentPlan.Sections, lSegmentPlan.Active);
    }

    public void LSegmentStartSet(TimeSpan lSegmentCursor, TimeSpan lSegmentDuration, int lSegmentColorIndex, bool lSegmentOverlapAllowed)
    {
        if (LPiece.LPieceStartSet(lSegmentPieces, lSegmentIndexActive, lSegmentCursor, lSegmentDuration, lSegmentColorIndex, lSegmentOverlapAllowed)
            is not { } lSegmentPlan) return;
        LSegmentApply(lSegmentPlan.Sections, lSegmentPlan.Active);
    }

    public void LSegmentEndSet(TimeSpan lSegmentCursor, int lSegmentColorIndex, bool lSegmentOverlapAllowed)
    {
        if (LPiece.LPieceEndSet(lSegmentPieces, lSegmentIndexActive, lSegmentCursor, lSegmentColorIndex, lSegmentOverlapAllowed)
            is not { } lSegmentPlan) return;
        LSegmentApply(lSegmentPlan.Sections, lSegmentPlan.Active);
    }

    public void LSegmentDivide(TimeSpan lSegmentCursor, int lSegmentColorIndex)
    {
        if (LPiece.LPieceDivide(lSegmentPieces, lSegmentIndexActive, lSegmentCursor, lSegmentColorIndex)
            is not { } lSegmentPlan) return;
        LSegmentApply(lSegmentPlan.Sections, lSegmentPlan.First);
    }

    public void LSegmentDelete()
    {
        if (lSegmentIndexActive is not int lSegmentIndex) return;
        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        lSegmentList.RemoveAt(lSegmentIndex);
        int? lSegmentSelect = lSegmentList.Count == 0 ? null : Math.Min(lSegmentIndex, lSegmentList.Count - 1);
        LSegmentApply(lSegmentList, lSegmentSelect);
    }

    public void LSegmentToggle(int lSegmentIndex)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;
        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        LPiece lSegmentPiece = lSegmentList[lSegmentIndex];
        lSegmentList[lSegmentIndex] = lSegmentPiece with { LPieceHidden = !lSegmentPiece.LPieceHidden };
        LSegmentApply(lSegmentList, lSegmentIndexActive);
    }

    public void LSegmentMove(int lSegmentSource, int lSegmentTarget)
    {
        if (lSegmentSource < 0 || lSegmentSource >= lSegmentPieces.Count) return;
        int lSegmentInsert = Math.Clamp(
            lSegmentSource < lSegmentTarget ? lSegmentTarget - 1 : lSegmentTarget,
            0,
            lSegmentPieces.Count - 1);
        if (lSegmentInsert == lSegmentSource) return;

        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        LPiece lSegmentMoved = lSegmentList[lSegmentSource];
        lSegmentList.RemoveAt(lSegmentSource);
        lSegmentList.Insert(lSegmentInsert, lSegmentMoved);
        int? lSegmentSelect = lSegmentIndexActive == lSegmentSource ? lSegmentInsert : lSegmentIndexActive;
        LSegmentApply(lSegmentList, lSegmentSelect);
    }

    public void LSegmentSort()
    {
        if (lSegmentPieces.Count < 2) return;

        LPiece? lSegmentSelected = lSegmentIndexActive is int lSegmentSelectIndex
            ? lSegmentPieces[lSegmentSelectIndex]
            : null;

        List<LPiece> lSegmentSorted = lSegmentPieces
            .OrderBy(lSegmentPiece => lSegmentPiece.LPieceName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (lSegmentSorted.SequenceEqual(lSegmentPieces)) return;

        int? lSegmentSelect = lSegmentIndexActive;
        if (lSegmentSelected is { } lSegmentKept)
        {
            int lSegmentIndexNew = lSegmentSorted.IndexOf(lSegmentKept);
            lSegmentSelect = lSegmentIndexNew < 0 ? null : lSegmentIndexNew;
        }

        LSegmentApply(lSegmentSorted, lSegmentSelect);
    }

    public void LSegmentNameSet(int lSegmentIndex, string lSegmentName, string? lSegmentPrefix, string? lSegmentSuffix)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;

        LPiece lSegmentPiece = lSegmentPieces[lSegmentIndex];
        string lSegmentPrefixNew = lSegmentPrefix ?? lSegmentPiece.LPiecePrefix;
        string lSegmentSuffixNew = lSegmentSuffix ?? lSegmentPiece.LPieceSuffix;
        if (string.Equals(lSegmentPiece.LPieceName, lSegmentName, StringComparison.Ordinal)
            && string.Equals(lSegmentPiece.LPiecePrefix, lSegmentPrefixNew, StringComparison.Ordinal)
            && string.Equals(lSegmentPiece.LPieceSuffix, lSegmentSuffixNew, StringComparison.Ordinal))
        {
            return;
        }

        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        lSegmentList[lSegmentIndex] = lSegmentPiece with
        {
            LPieceName = lSegmentName,
            LPiecePrefix = lSegmentPrefixNew,
            LPieceSuffix = lSegmentSuffixNew
        };
        LSegmentApply(lSegmentList, lSegmentIndexActive);
    }

    public void LSegmentSelect(int lSegmentIndex)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;
        lSegmentIndexActive = lSegmentIndex;
        LSegmentChange?.Invoke(lSegmentPieces.ToArray(), lSegmentIndexActive);
    }

    private void LSegmentApply(List<LPiece> lSegmentSections, int? lSegmentSelect)
    {
        lSegmentPieces.Clear();
        lSegmentPieces.AddRange(lSegmentSections);
        lSegmentIndexActive = lSegmentSelect;
        LSegmentChange?.Invoke(lSegmentPieces.ToArray(), lSegmentIndexActive);
    }
}
