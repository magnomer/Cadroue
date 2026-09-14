using Cadroue.Core;

namespace Cadroue.Application;

public enum LSegmentFault
{
    LSegmentFaultCeiling,
    LSegmentFaultStorage,
    LSegmentFaultInvalid
}

public sealed class LSegment
{
    private readonly List<LPiece> lSegmentPieces = new();
    private readonly SortedSet<int> lSegmentIndexSelected = new();
    private int? lSegmentIndexActive;
    private string? lSegmentSourcePath;
    private bool lSegmentRestoring;
    private int lSegmentVersion;

    public Action<IReadOnlyList<LPiece>, int?>? LSegmentNotice;
    public Action<LSegmentFault, int>? LSegmentFaultNotice;

    public static Func<string, IReadOnlyList<LSidecarSectionRecord>>? LSegmentLoadSeam;
    public static Func<string, IReadOnlyList<LSidecarSectionRecord>, bool>? LSegmentSaveSeam;

    public IReadOnlyList<LPiece> LSegmentListRead() => lSegmentPieces.ToArray();

    public int LSegmentVersionRead() => lSegmentVersion;

    public int? LSegmentSelectionRead() => lSegmentIndexActive;

    public IReadOnlyList<int> LSegmentSelectedRead() => lSegmentIndexSelected.ToArray();

    public string? LSegmentSourceRead() => lSegmentSourcePath;

    public void LSegmentSourceSet(string? lSegmentSource) => lSegmentSourcePath = lSegmentSource;

    public void LSegmentReset()
    {
        lSegmentPieces.Clear();
        lSegmentIndexSelected.Clear();
        lSegmentIndexActive = null;
    }

    public void LSegmentLoad(TimeSpan lSegmentDuration)
    {
        if (lSegmentSourcePath is not { } lSegmentSource || lSegmentPieces.Count > 0) return;

        IReadOnlyList<LSidecarSectionRecord> lSegmentRecords =
            LSegmentLoadSeam?.Invoke(lSegmentSource) ?? Array.Empty<LSidecarSectionRecord>();
        if (lSegmentRecords.Count == 0) return;

        IReadOnlyList<LPiece> lSegmentList = LPiece.LPieceValidSelect(
            lSegmentRecords.Select(LPiece.LPieceCreate).ToArray(),
            lSegmentDuration);
        if (lSegmentList.Count < lSegmentRecords.Count)
        {
            LSegmentFaultNotice?.Invoke(LSegmentFault.LSegmentFaultInvalid, lSegmentRecords.Count - lSegmentList.Count);
        }

        lSegmentRestoring = true;
        try
        {
            LSegmentApply(lSegmentList.ToList(), null);
        }
        finally
        {
            lSegmentRestoring = false;
        }
    }

    public IReadOnlyList<LSidecarSectionRecord> LSegmentRecordsRead() =>
        lSegmentPieces.Select(lSegmentPiece => lSegmentPiece.LPieceRecordCreate()).ToArray();

    private bool LSegmentSave()
    {
        if (lSegmentRestoring || lSegmentSourcePath is not { } lSegmentSource) return true;
        return LSegmentSaveSeam?.Invoke(lSegmentSource, LSegmentRecordsRead()) ?? true;
    }

    public bool LSegmentSet(IReadOnlyList<LPiece> lSegmentSections, int? lSegmentSelect)
    {
        List<LPiece> lSegmentList = lSegmentSections.ToList();
        int? lSegmentClamp = lSegmentList.Count == 0 || lSegmentSelect is not int lSelect
            ? null
            : Math.Clamp(lSelect, 0, lSegmentList.Count - 1);
        return LSegmentApply(lSegmentList, lSegmentClamp);
    }

    public bool LSegmentBoundSet(IReadOnlyList<LPiece> lSegmentSections, int? lSegmentSelect, TimeSpan lSegmentDuration)
    {
        IReadOnlyList<LPiece> lSegmentValid = LPiece.LPieceValidSelect(lSegmentSections, lSegmentDuration);
        return LSegmentSet(lSegmentValid, lSegmentSelect);
    }

    public void LSegmentLosslesscutSet(IReadOnlyList<LSidecarSectionRecord> lSegmentSections, int lSegmentPaletteCount)
    {
        List<LPiece> lSegmentImported = LSegmentLosslesscutCreate(lSegmentSections, 0, lSegmentPaletteCount);
        LSegmentApply(lSegmentImported, lSegmentImported.Count > 0 ? 0 : null);
    }

    public void LSegmentLosslesscutAppend(
        IReadOnlyList<LSidecarSectionRecord> lSegmentSections,
        int lSegmentPaletteCount)
    {
        int lSegmentFirst = lSegmentPieces.Count;
        List<LPiece> lSegmentImported = LSegmentLosslesscutCreate(
            lSegmentSections,
            lSegmentFirst,
            lSegmentPaletteCount);
        if (lSegmentImported.Count == 0) return;
        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        lSegmentList.AddRange(lSegmentImported);
        LSegmentApply(lSegmentList, lSegmentFirst);
    }

    private static List<LPiece> LSegmentLosslesscutCreate(
        IReadOnlyList<LSidecarSectionRecord> lSegmentSections,
        int lSegmentColorOffset,
        int lSegmentPaletteCount)
    {
        int lSegmentPalette = Math.Max(1, lSegmentPaletteCount);
        return lSegmentSections
            .Select((lSegmentSection, lSegmentIndex) => LPiece.LPieceCreate(lSegmentSection) with
            {
                LPieceColorIndex = (lSegmentColorOffset + lSegmentIndex) % lSegmentPalette
            })
            .ToList();
    }

    public void LSegmentAdd(
        TimeSpan lSegmentCursor,
        TimeSpan lSegmentDuration,
        int lSegmentColorIndex,
        bool lSegmentOverlapAllowed)
    {
        if (LPiece.LPieceAdd(
                lSegmentPieces,
                lSegmentCursor,
                lSegmentDuration,
                lSegmentColorIndex,
                lSegmentOverlapAllowed)
            is not { } lSegmentPlan) return;
        LSegmentApply(lSegmentPlan.LPieceSections, lSegmentPlan.LPieceActive);
    }

    public bool? LSegmentStartSet(
        TimeSpan lSegmentCursor,
        TimeSpan lSegmentDuration,
        int lSegmentColorIndex,
        bool lSegmentOverlapAllowed)
    {
        if (LPiece.LPieceOriginSet(
                lSegmentPieces,
                lSegmentIndexActive,
                lSegmentCursor,
                lSegmentDuration,
                lSegmentColorIndex,
                lSegmentOverlapAllowed)
            is not { } lSegmentPlan) return null;
        return LSegmentApply(lSegmentPlan.LPieceSections, lSegmentPlan.LPieceActive)
            ? lSegmentPlan.LPieceAdded
            : null;
    }

    public bool? LSegmentEndSet(TimeSpan lSegmentCursor, int lSegmentColorIndex, bool lSegmentOverlapAllowed)
    {
        if (LPiece.LPieceEndSet(
                lSegmentPieces,
                lSegmentIndexActive,
                lSegmentCursor,
                lSegmentColorIndex,
                lSegmentOverlapAllowed)
            is not { } lSegmentPlan) return null;
        return LSegmentApply(lSegmentPlan.LPieceSections, lSegmentPlan.LPieceActive)
            ? lSegmentPlan.LPieceAdded
            : null;
    }

    public void LSegmentDivide(TimeSpan lSegmentCursor, int lSegmentColorIndex)
    {
        if (LPiece.LPieceDivide(lSegmentPieces, lSegmentIndexActive, lSegmentCursor, lSegmentColorIndex)
            is not { } lSegmentPlan) return;
        LSegmentApply(lSegmentPlan.LPieceSections, lSegmentPlan.LPieceFirst);
    }

    public bool LSegmentDelete(IReadOnlyList<int> lSegmentIndexes, int lSegmentApproved)
    {
        if (lSegmentApproved != lSegmentVersion || lSegmentIndexes.Count == 0) return false;
        var lSegmentDeleted = new HashSet<int>(lSegmentIndexes);
        List<LPiece> lSegmentList = lSegmentPieces
            .Where((_, lSegmentIndex) => !lSegmentDeleted.Contains(lSegmentIndex))
            .ToList();
        int? lSegmentSelect = lSegmentList.Count == 0 ? null : Math.Min(lSegmentIndexes.Min(), lSegmentList.Count - 1);
        return LSegmentApply(lSegmentList, lSegmentSelect);
    }

    public bool LSegmentClear(int lSegmentApproved)
    {
        if (lSegmentApproved != lSegmentVersion || lSegmentPieces.Count == 0) return false;
        return LSegmentApply(new List<LPiece>(), null);
    }

    public void LSegmentToggle(int lSegmentIndex)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;
        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        LPiece lSegmentPiece = lSegmentList[lSegmentIndex];
        lSegmentList[lSegmentIndex] = lSegmentPiece with
        {
            LPieceHidden = !lSegmentPiece.LPieceHidden,
            LPieceDetected = false
        };
        LSegmentApply(lSegmentList, lSegmentIndexActive);
    }

    public int? LSegmentMove(int lSegmentSource, int lSegmentTarget)
    {
        if (lSegmentSource < 0 || lSegmentSource >= lSegmentPieces.Count) return null;
        int lSegmentInsert = Math.Clamp(
            lSegmentSource < lSegmentTarget ? lSegmentTarget - 1 : lSegmentTarget,
            0,
            lSegmentPieces.Count - 1);
        if (lSegmentInsert == lSegmentSource) return null;

        List<LPiece> lSegmentList = lSegmentPieces.ToList();
        LPiece lSegmentMoved = lSegmentList[lSegmentSource];
        lSegmentList.RemoveAt(lSegmentSource);
        lSegmentList.Insert(lSegmentInsert, lSegmentMoved);
        int? lSegmentSelect = lSegmentIndexActive == lSegmentSource ? lSegmentInsert : lSegmentIndexActive;
        LSegmentApply(lSegmentList, lSegmentSelect);
        return lSegmentInsert;
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
            LPieceSuffix = lSegmentSuffixNew,
            LPieceDetected = false
        };
        LSegmentApply(lSegmentList, lSegmentIndexActive);
    }

    public void LSegmentSelect(int lSegmentIndex)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;
        lSegmentIndexActive = lSegmentIndex;
        lSegmentIndexSelected.Clear();
        lSegmentIndexSelected.Add(lSegmentIndex);
        LSegmentNotice?.Invoke(lSegmentPieces.ToArray(), lSegmentIndexActive);
    }

    public void LSegmentSelectToggle(int lSegmentIndex)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;
        if (!lSegmentIndexSelected.Remove(lSegmentIndex))
        {
            lSegmentIndexSelected.Add(lSegmentIndex);
            lSegmentIndexActive = lSegmentIndex;
        }
        else if (lSegmentIndexActive == lSegmentIndex)
        {
            lSegmentIndexActive = lSegmentIndexSelected.Count == 0 ? null : lSegmentIndexSelected.Max;
        }

        LSegmentNotice?.Invoke(lSegmentPieces.ToArray(), lSegmentIndexActive);
    }

    public void LSegmentRangeSelect(int lSegmentIndex)
    {
        if (lSegmentIndex < 0 || lSegmentIndex >= lSegmentPieces.Count) return;
        int lSegmentAnchor = lSegmentIndexActive ?? lSegmentIndex;
        int lSegmentLow = Math.Min(lSegmentAnchor, lSegmentIndex);
        int lSegmentHigh = Math.Max(lSegmentAnchor, lSegmentIndex);
        lSegmentIndexSelected.Clear();
        for (int lSegmentStep = lSegmentLow; lSegmentStep <= lSegmentHigh; lSegmentStep++)
        {
            lSegmentIndexSelected.Add(lSegmentStep);
        }

        LSegmentNotice?.Invoke(lSegmentPieces.ToArray(), lSegmentIndexActive);
    }

    private bool LSegmentApply(List<LPiece> lSegmentSections, int? lSegmentSelect)
    {
        if (lSegmentSections.Count > LPiece.LPieceCeiling)
        {
            LSegmentFaultNotice?.Invoke(LSegmentFault.LSegmentFaultCeiling, lSegmentSections.Count);
            return false;
        }

        lSegmentPieces.Clear();
        lSegmentPieces.AddRange(lSegmentSections);
        lSegmentIndexActive = lSegmentSelect;
        lSegmentIndexSelected.Clear();
        if (lSegmentSelect is int lSegmentActive)
        {
            lSegmentIndexSelected.Add(lSegmentActive);
        }

        lSegmentVersion++;
        LSegmentNotice?.Invoke(lSegmentPieces.ToArray(), lSegmentIndexActive);
        if (!LSegmentSave())
        {
            LSegmentFaultNotice?.Invoke(LSegmentFault.LSegmentFaultStorage, lSegmentPieces.Count);
        }

        return true;
    }
}
