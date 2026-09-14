namespace Cadroue.Core;

public sealed record LPieceResult(List<LPiece> LPieceSections, int? LPieceActive, bool LPieceAdded = false);

public sealed record LPieceDivision(List<LPiece> LPieceSections, int LPieceFirst, int LPieceSecond);

public readonly partial record struct LPiece
{
    public static IReadOnlyList<LPiece> LPieceValidSelect(IReadOnlyList<LPiece> lPieceSections, TimeSpan lPieceDuration)
    {
        List<LPiece> lPieceValid = new();
        foreach (LPiece lPieceSection in lPieceSections)
        {
            if (lPieceSection.LPieceOrigin >= TimeSpan.Zero
                && lPieceSection.LPieceOrigin < lPieceSection.LPieceEnd
                && lPieceSection.LPieceEnd <= lPieceDuration)
            {
                lPieceValid.Add(lPieceSection with { LPieceColorIndex = Math.Max(0, lPieceSection.LPieceColorIndex) });
            }
        }

        return lPieceValid;
    }

    public static bool LPieceInsideCheck(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceTime,
        int lPieceSkipIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceOverlapAllowed)
        {
            return false;
        }

        for (int lPieceIndex = 0; lPieceIndex < lPieces.Count; lPieceIndex++)
        {
            if (lPieceIndex == lPieceSkipIndex)
            {
                continue;
            }

            LPiece lPiece = lPieces[lPieceIndex];
            if (lPieceTime >= lPiece.LPieceOrigin && lPieceTime < lPiece.LPieceEnd)
            {
                return true;
            }
        }

        return false;
    }

    public static bool LPieceIntersectCheck(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceOrigin,
        TimeSpan lPieceEnd,
        int lPieceSkipIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceOverlapAllowed)
        {
            return false;
        }

        for (int lPieceIndex = 0; lPieceIndex < lPieces.Count; lPieceIndex++)
        {
            if (lPieceIndex == lPieceSkipIndex)
            {
                continue;
            }

            LPiece lPiece = lPieces[lPieceIndex];
            if (lPieceOrigin < lPiece.LPieceEnd && lPieceEnd > lPiece.LPieceOrigin)
            {
                return true;
            }
        }

        return false;
    }

    public static TimeSpan LPieceLimitRead(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceFrom,
        TimeSpan lPieceCeiling,
        int lPieceSkipIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceOverlapAllowed)
        {
            return lPieceCeiling;
        }

        TimeSpan lPieceLimit = lPieceCeiling;
        for (int lPieceIndex = 0; lPieceIndex < lPieces.Count; lPieceIndex++)
        {
            if (lPieceIndex == lPieceSkipIndex)
            {
                continue;
            }

            TimeSpan lPieceOrigin = lPieces[lPieceIndex].LPieceOrigin;
            if (lPieceOrigin > lPieceFrom && lPieceOrigin < lPieceLimit)
            {
                lPieceLimit = lPieceOrigin;
            }
        }

        return lPieceLimit;
    }

    public static TimeSpan LPieceFloorRead(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceUntil,
        int lPieceSkipIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceOverlapAllowed)
        {
            return TimeSpan.Zero;
        }

        TimeSpan lPieceFloor = TimeSpan.Zero;
        for (int lPieceIndex = 0; lPieceIndex < lPieces.Count; lPieceIndex++)
        {
            if (lPieceIndex == lPieceSkipIndex)
            {
                continue;
            }

            TimeSpan lPieceEnd = lPieces[lPieceIndex].LPieceEnd;
            if (lPieceEnd <= lPieceUntil && lPieceEnd > lPieceFloor)
            {
                lPieceFloor = lPieceEnd;
            }
        }

        return lPieceFloor;
    }

    public static LPieceResult? LPieceAdd(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceCursor,
        TimeSpan lPieceDuration,
        int lPieceColorIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceCursor >= lPieceDuration)
        {
            return null;
        }

        TimeSpan lPieceEnd = LPieceLimitRead(lPieces, lPieceCursor, lPieceDuration, -1, lPieceOverlapAllowed);
        return LPieceAppend(lPieces, lPieceCursor, lPieceEnd, lPieceColorIndex, lPieceOverlapAllowed);
    }

    private static LPieceResult? LPieceAppend(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceOrigin,
        TimeSpan lPieceEnd,
        int lPieceColorIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceEnd <= lPieceOrigin
            || LPieceIntersectCheck(lPieces, lPieceOrigin, lPieceEnd, -1, lPieceOverlapAllowed))
        {
            return null;
        }

        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList.Add(new LPiece(lPieceOrigin, lPieceEnd, lPieceColorIndex, string.Empty));
        return new LPieceResult(lPieceList, lPieceList.Count - 1);
    }

    private static LPieceResult? LPieceSet(
        IReadOnlyList<LPiece> lPieces,
        int lPieceIndex,
        LPiece lPiece,
        bool lPieceOverlapAllowed)
    {
        if (LPieceIntersectCheck(lPieces, lPiece.LPieceOrigin, lPiece.LPieceEnd, lPieceIndex, lPieceOverlapAllowed))
        {
            return null;
        }

        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList[lPieceIndex] = lPiece;
        return new LPieceResult(lPieceList, lPieceIndex);
    }

    public static LPieceResult? LPieceEndCreate(
        IReadOnlyList<LPiece> lPieces,
        TimeSpan lPieceCursor,
        int lPieceColorIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceCursor <= TimeSpan.Zero)
        {
            return null;
        }

        TimeSpan lPieceOrigin = LPieceFloorRead(lPieces, lPieceCursor, -1, lPieceOverlapAllowed);
        return LPieceAppend(lPieces, lPieceOrigin, lPieceCursor, lPieceColorIndex, lPieceOverlapAllowed);
    }

    public static LPieceResult? LPieceOriginSet(
        IReadOnlyList<LPiece> lPieces,
        int? lPieceActiveIndex,
        TimeSpan lPieceCursor,
        TimeSpan lPieceDuration,
        int lPieceColorIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceActiveIndex is null
            || lPieceCursor >= lPieces[lPieceActiveIndex.Value].LPieceEnd)
        {
            return LPieceAdd(lPieces, lPieceCursor, lPieceDuration, lPieceColorIndex, lPieceOverlapAllowed)
                is { } lPieceAddPlan
                ? lPieceAddPlan with { LPieceAdded = true }
                : null;
        }

        LPiece lPiece = lPieces[lPieceActiveIndex.Value];
        return LPieceSet(
            lPieces,
            lPieceActiveIndex.Value,
            lPiece with { LPieceOrigin = lPieceCursor, LPieceDetected = false },
            lPieceOverlapAllowed);
    }

    public static LPieceResult? LPieceEndSet(
        IReadOnlyList<LPiece> lPieces,
        int? lPieceActiveIndex,
        TimeSpan lPieceCursor,
        int lPieceColorIndex,
        bool lPieceOverlapAllowed)
    {
        if (lPieceActiveIndex is null)
        {
            return LPieceEndCreate(lPieces, lPieceCursor, lPieceColorIndex, lPieceOverlapAllowed)
                is { } lPieceEndPlan
                ? lPieceEndPlan with { LPieceAdded = true }
                : null;
        }

        LPiece lPiece = lPieces[lPieceActiveIndex.Value];
        if (lPieceCursor <= lPiece.LPieceOrigin)
        {
            return LPieceEndCreate(lPieces, lPieceCursor, lPieceColorIndex, lPieceOverlapAllowed)
                is { } lPieceEndPlan
                ? lPieceEndPlan with { LPieceAdded = true }
                : null;
        }

        return LPieceSet(
            lPieces,
            lPieceActiveIndex.Value,
            lPiece with { LPieceEnd = lPieceCursor, LPieceDetected = false },
            lPieceOverlapAllowed);
    }

    public static LPieceDivision? LPieceDivide(
        IReadOnlyList<LPiece> lPieces,
        int? lPieceActiveIndex,
        TimeSpan lPieceCursor,
        int lPieceColorIndex)
    {
        if (lPieceActiveIndex is null)
        {
            return null;
        }

        LPiece lPiece = lPieces[lPieceActiveIndex.Value];
        if (lPieceCursor <= lPiece.LPieceOrigin || lPieceCursor >= lPiece.LPieceEnd)
        {
            return null;
        }

        int lPieceIndex = lPieceActiveIndex.Value;
        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList.RemoveAt(lPieceIndex);
        lPieceList.Insert(lPieceIndex, new LPiece(lPieceCursor, lPiece.LPieceEnd, lPieceColorIndex, string.Empty));
        lPieceList.Insert(lPieceIndex, lPiece with { LPieceEnd = lPieceCursor, LPieceDetected = false });
        return new LPieceDivision(lPieceList, lPieceIndex, lPieceIndex + 1);
    }
}
