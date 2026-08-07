using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LSegment
{
    private readonly List<LPiece> lSegmentPieces = new();
    private int? lSegmentIndexActive;
    private string? lSegmentSourcePath;
    private bool lSegmentRestoring;

    public Action<IReadOnlyList<LPiece>, int?>? LSegmentChange;

    public static Func<string, IReadOnlyList<LSidecarSectionRecord>>? LSegmentLoadSeam;
    public static Action<string, IReadOnlyList<LSidecarSectionRecord>>? LSegmentSaveSeam;

    public IReadOnlyList<LPiece> LSegmentListRead() => lSegmentPieces.ToArray();

    public int? LSegmentSelectionRead() => lSegmentIndexActive;

    public string? LSegmentSourceRead() => lSegmentSourcePath;

    public void LSegmentSourceSet(string? lSegmentSource) => lSegmentSourcePath = lSegmentSource;

    public void LSegmentReset()
    {
        lSegmentPieces.Clear();
        lSegmentIndexActive = null;
    }

    public void LSegmentLoad(TimeSpan lSegmentDuration)
    {
        if (lSegmentSourcePath is not { } lSegmentSource || lSegmentPieces.Count > 0) return;

        IReadOnlyList<LSidecarSectionRecord> lSegmentRecords =
            LSegmentLoadSeam?.Invoke(lSegmentSource) ?? Array.Empty<LSidecarSectionRecord>();
        if (lSegmentRecords.Count == 0) return;

        List<LPiece> lSegmentList = new();
        foreach (LSidecarSectionRecord lSegmentRecord in lSegmentRecords)
        {
            LPiece lSegmentPiece = LSegmentPieceCreate(lSegmentRecord);
            if (lSegmentPiece.LPieceEnd <= lSegmentDuration && lSegmentPiece.LPieceStart < lSegmentPiece.LPieceEnd)
            {
                lSegmentList.Add(lSegmentPiece);
            }
        }

        lSegmentRestoring = true;
        try
        {
            LSegmentApply(lSegmentList, null);
        }
        finally
        {
            lSegmentRestoring = false;
        }
    }

    public IReadOnlyList<LSidecarSectionRecord> LSegmentRecordsRead() =>
        lSegmentPieces.Select(LSegmentRecordCreate).ToArray();

    private static LSidecarSectionRecord LSegmentRecordCreate(LPiece lSegmentPiece) => new()
    {
        LSidecarStartMilliseconds = (long)lSegmentPiece.LPieceStart.TotalMilliseconds,
        LSidecarEndMilliseconds = (long)lSegmentPiece.LPieceEnd.TotalMilliseconds,
        LSidecarColorIndex = lSegmentPiece.LPieceColorIndex,
        LSidecarName = lSegmentPiece.LPieceName,
        LSidecarPrefix = lSegmentPiece.LPiecePrefix,
        LSidecarSuffix = lSegmentPiece.LPieceSuffix,
        LSidecarHidden = lSegmentPiece.LPieceHidden
    };

    private static LPiece LSegmentPieceCreate(LSidecarSectionRecord lSegmentRecord) =>
        new(
            TimeSpan.FromMilliseconds(lSegmentRecord.LSidecarStartMilliseconds),
            TimeSpan.FromMilliseconds(lSegmentRecord.LSidecarEndMilliseconds),
            lSegmentRecord.LSidecarColorIndex,
            lSegmentRecord.LSidecarName)
        {
            LPiecePrefix = lSegmentRecord.LSidecarPrefix ?? string.Empty,
            LPieceSuffix = lSegmentRecord.LSidecarSuffix ?? string.Empty,
            LPieceHidden = lSegmentRecord.LSidecarHidden
        };

    private void LSegmentSave()
    {
        if (lSegmentRestoring || lSegmentSourcePath is not { } lSegmentSource) return;
        LSegmentSaveSeam?.Invoke(lSegmentSource, LSegmentRecordsRead());
    }

    public void LSegmentSet(IReadOnlyList<LPiece> lSegmentSections, int? lSegmentSelect)
    {
        List<LPiece> lSegmentList = lSegmentSections.ToList();
        int? lSegmentClamp = lSegmentList.Count == 0 || lSegmentSelect is not int lSelect
            ? null
            : Math.Clamp(lSelect, 0, lSegmentList.Count - 1);
        LSegmentApply(lSegmentList, lSegmentClamp);
    }

    public void LSegmentLosslesscutSet(IReadOnlyList<LSidecarSectionRecord> lSegmentSections, int lSegmentPaletteCount)
    {
        List<LPiece> lSegmentImported = LSegmentLosslesscutCreate(lSegmentSections, 0, lSegmentPaletteCount);
        LSegmentApply(lSegmentImported, lSegmentImported.Count > 0 ? 0 : null);
    }

    public void LSegmentLosslesscutAppend(IReadOnlyList<LSidecarSectionRecord> lSegmentSections, int lSegmentPaletteCount)
    {
        int lSegmentFirst = lSegmentPieces.Count;
        List<LPiece> lSegmentImported = LSegmentLosslesscutCreate(lSegmentSections, lSegmentFirst, lSegmentPaletteCount);
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
            .Select((lSegmentSection, lSegmentIndex) => new LPiece(
                TimeSpan.FromMilliseconds(lSegmentSection.LSidecarStartMilliseconds),
                TimeSpan.FromMilliseconds(lSegmentSection.LSidecarEndMilliseconds),
                (lSegmentColorOffset + lSegmentIndex) % lSegmentPalette,
                lSegmentSection.LSidecarName ?? string.Empty))
            .ToList();
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
        LSegmentSave();
    }
}
