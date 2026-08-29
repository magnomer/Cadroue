namespace Cadroue.Core;

public readonly partial record struct LPiece
{
    public static IReadOnlyList<LPiece> LPieceValidSelect(IReadOnlyList<LPiece> lPieceSections, TimeSpan lPieceDuration)
    {
        List<LPiece> lPieceValid = new();
        foreach (LPiece lPieceSection in lPieceSections)
        {
            if (lPieceSection.LPieceEnd <= lPieceDuration && lPieceSection.LPieceOrigin < lPieceSection.LPieceEnd)
            {
                lPieceValid.Add(lPieceSection);
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

    public static (List<LPiece> Sections, int? Active)? LPieceAdd(
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

        if (LPieceInsideCheck(lPieces, lPieceCursor, -1, lPieceOverlapAllowed))
        {
            return null;
        }

        TimeSpan lPieceEnd = LPieceLimitRead(lPieces, lPieceCursor, lPieceDuration, -1, lPieceOverlapAllowed);
        if (lPieceEnd <= lPieceCursor)
        {
            return null;
        }

        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList.Add(new LPiece(lPieceCursor, lPieceEnd, lPieceColorIndex, string.Empty));
        return (lPieceList, lPieceList.Count - 1);
    }

    public static (List<LPiece> Sections, int? Active)? LPieceEndCreate(
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
        if (lPieceOrigin >= lPieceCursor)
        {
            return null;
        }

        if (lPieceCursor > LPieceLimitRead(lPieces, lPieceOrigin, lPieceCursor, -1, lPieceOverlapAllowed))
        {
            return null;
        }

        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList.Add(new LPiece(lPieceOrigin, lPieceCursor, lPieceColorIndex, string.Empty));
        return (lPieceList, lPieceList.Count - 1);
    }

    public static (List<LPiece> Sections, int? Active, bool Added)? LPieceOriginSet(
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
                ? (lPieceAddPlan.Sections, lPieceAddPlan.Active, true)
                : null;
        }

        LPiece lPiece = lPieces[lPieceActiveIndex.Value];
        if (lPieceCursor < LPieceFloorRead(lPieces, lPiece.LPieceOrigin, lPieceActiveIndex.Value, lPieceOverlapAllowed))
        {
            return null;
        }

        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList[lPieceActiveIndex.Value] = lPiece with { LPieceOrigin = lPieceCursor, LPieceDetected = false };
        return (lPieceList, lPieceActiveIndex, false);
    }

    public static (List<LPiece> Sections, int? Active, bool Added)? LPieceEndSet(
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
                ? (lPieceEndPlan.Sections, lPieceEndPlan.Active, true)
                : null;
        }

        LPiece lPiece = lPieces[lPieceActiveIndex.Value];
        if (lPieceCursor <= lPiece.LPieceOrigin)
        {
            return null;
        }

        if (lPieceCursor > LPieceLimitRead(lPieces, lPiece.LPieceEnd, lPieceCursor, lPieceActiveIndex.Value, lPieceOverlapAllowed))
        {
            return null;
        }

        List<LPiece> lPieceList = lPieces.ToList();
        lPieceList[lPieceActiveIndex.Value] = lPiece with { LPieceEnd = lPieceCursor, LPieceDetected = false };
        return (lPieceList, lPieceActiveIndex, false);
    }

    public static (List<LPiece> Sections, int First, int Second)? LPieceDivide(
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
        return (lPieceList, lPieceIndex, lPieceIndex + 1);
    }
}
