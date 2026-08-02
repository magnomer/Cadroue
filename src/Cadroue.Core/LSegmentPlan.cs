namespace Cadroue.Core;

public readonly partial record struct LSegment
{
    public static bool LSegmentInsideCheck(
        IReadOnlyList<LSegment> lSegments,
        TimeSpan lSegmentTime,
        int lSegmentSkipIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentOverlapAllowed)
        {
            return false;
        }

        for (int lSegmentIndex = 0; lSegmentIndex < lSegments.Count; lSegmentIndex++)
        {
            if (lSegmentIndex == lSegmentSkipIndex)
            {
                continue;
            }

            LSegment lSegment = lSegments[lSegmentIndex];
            if (lSegmentTime >= lSegment.LSegmentStart && lSegmentTime < lSegment.LSegmentEnd)
            {
                return true;
            }
        }

        return false;
    }

    public static TimeSpan LSegmentLimitRead(
        IReadOnlyList<LSegment> lSegments,
        TimeSpan lSegmentFrom,
        TimeSpan lSegmentCeiling,
        int lSegmentSkipIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentOverlapAllowed)
        {
            return lSegmentCeiling;
        }

        TimeSpan lSegmentLimit = lSegmentCeiling;
        for (int lSegmentIndex = 0; lSegmentIndex < lSegments.Count; lSegmentIndex++)
        {
            if (lSegmentIndex == lSegmentSkipIndex)
            {
                continue;
            }

            TimeSpan lSegmentStart = lSegments[lSegmentIndex].LSegmentStart;
            if (lSegmentStart > lSegmentFrom && lSegmentStart < lSegmentLimit)
            {
                lSegmentLimit = lSegmentStart;
            }
        }

        return lSegmentLimit;
    }

    public static TimeSpan LSegmentFloorRead(
        IReadOnlyList<LSegment> lSegments,
        TimeSpan lSegmentUntil,
        int lSegmentSkipIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentOverlapAllowed)
        {
            return TimeSpan.Zero;
        }

        TimeSpan lSegmentFloor = TimeSpan.Zero;
        for (int lSegmentIndex = 0; lSegmentIndex < lSegments.Count; lSegmentIndex++)
        {
            if (lSegmentIndex == lSegmentSkipIndex)
            {
                continue;
            }

            TimeSpan lSegmentEnd = lSegments[lSegmentIndex].LSegmentEnd;
            if (lSegmentEnd <= lSegmentUntil && lSegmentEnd > lSegmentFloor)
            {
                lSegmentFloor = lSegmentEnd;
            }
        }

        return lSegmentFloor;
    }

    public static (List<LSegment> Sections, int? Active)? LSegmentAdd(
        IReadOnlyList<LSegment> lSegments,
        TimeSpan lSegmentCursor,
        TimeSpan lSegmentDuration,
        int lSegmentColorIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentCursor >= lSegmentDuration)
        {
            return null;
        }

        if (LSegmentInsideCheck(lSegments, lSegmentCursor, -1, lSegmentOverlapAllowed))
        {
            return null;
        }

        TimeSpan lSegmentEnd = LSegmentLimitRead(lSegments, lSegmentCursor, lSegmentDuration, -1, lSegmentOverlapAllowed);
        if (lSegmentEnd <= lSegmentCursor)
        {
            return null;
        }

        List<LSegment> lSegmentList = lSegments.ToList();
        lSegmentList.Add(new LSegment(lSegmentCursor, lSegmentEnd, lSegmentColorIndex, string.Empty));
        return (lSegmentList, lSegmentList.Count - 1);
    }

    public static (List<LSegment> Sections, int? Active)? LSegmentEndCreate(
        IReadOnlyList<LSegment> lSegments,
        TimeSpan lSegmentCursor,
        int lSegmentColorIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentCursor <= TimeSpan.Zero)
        {
            return null;
        }

        TimeSpan lSegmentStart = LSegmentFloorRead(lSegments, lSegmentCursor, -1, lSegmentOverlapAllowed);
        if (lSegmentStart >= lSegmentCursor)
        {
            return null;
        }

        if (lSegmentCursor > LSegmentLimitRead(lSegments, lSegmentStart, lSegmentCursor, -1, lSegmentOverlapAllowed))
        {
            return null;
        }

        List<LSegment> lSegmentList = lSegments.ToList();
        lSegmentList.Add(new LSegment(lSegmentStart, lSegmentCursor, lSegmentColorIndex, string.Empty));
        return (lSegmentList, lSegmentList.Count - 1);
    }

    public static (List<LSegment> Sections, int? Active)? LSegmentStartSet(
        IReadOnlyList<LSegment> lSegments,
        int? lSegmentActiveIndex,
        TimeSpan lSegmentCursor,
        TimeSpan lSegmentDuration,
        int lSegmentColorIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentActiveIndex is null)
        {
            return LSegmentAdd(lSegments, lSegmentCursor, lSegmentDuration, lSegmentColorIndex, lSegmentOverlapAllowed);
        }

        LSegment lSegment = lSegments[lSegmentActiveIndex.Value];
        if (lSegment.LSegmentEnd < lSegmentCursor)
        {
            return LSegmentAdd(lSegments, lSegmentCursor, lSegmentDuration, lSegmentColorIndex, lSegmentOverlapAllowed);
        }

        if (lSegmentCursor >= lSegment.LSegmentEnd)
        {
            return null;
        }

        if (lSegmentCursor < LSegmentFloorRead(lSegments, lSegment.LSegmentStart, lSegmentActiveIndex.Value, lSegmentOverlapAllowed))
        {
            return null;
        }

        List<LSegment> lSegmentList = lSegments.ToList();
        lSegmentList[lSegmentActiveIndex.Value] = lSegment with { LSegmentStart = lSegmentCursor };
        return (lSegmentList, lSegmentActiveIndex);
    }

    public static (List<LSegment> Sections, int? Active)? LSegmentEndSet(
        IReadOnlyList<LSegment> lSegments,
        int? lSegmentActiveIndex,
        TimeSpan lSegmentCursor,
        int lSegmentColorIndex,
        bool lSegmentOverlapAllowed)
    {
        if (lSegmentActiveIndex is null)
        {
            return LSegmentEndCreate(lSegments, lSegmentCursor, lSegmentColorIndex, lSegmentOverlapAllowed);
        }

        LSegment lSegment = lSegments[lSegmentActiveIndex.Value];
        if (lSegmentCursor <= lSegment.LSegmentStart)
        {
            return null;
        }

        if (lSegmentCursor > LSegmentLimitRead(lSegments, lSegment.LSegmentEnd, lSegmentCursor, lSegmentActiveIndex.Value, lSegmentOverlapAllowed))
        {
            return null;
        }

        List<LSegment> lSegmentList = lSegments.ToList();
        lSegmentList[lSegmentActiveIndex.Value] = lSegment with { LSegmentEnd = lSegmentCursor };
        return (lSegmentList, lSegmentActiveIndex);
    }

    public static (List<LSegment> Sections, int First, int Second)? LSegmentDivide(
        IReadOnlyList<LSegment> lSegments,
        int? lSegmentActiveIndex,
        TimeSpan lSegmentCursor,
        int lSegmentColorIndex)
    {
        if (lSegmentActiveIndex is null)
        {
            return null;
        }

        LSegment lSegment = lSegments[lSegmentActiveIndex.Value];
        if (lSegmentCursor <= lSegment.LSegmentStart || lSegmentCursor >= lSegment.LSegmentEnd)
        {
            return null;
        }

        int lSegmentIndex = lSegmentActiveIndex.Value;
        List<LSegment> lSegmentList = lSegments.ToList();
        lSegmentList.RemoveAt(lSegmentIndex);
        lSegmentList.Insert(lSegmentIndex, new LSegment(lSegmentCursor, lSegment.LSegmentEnd, lSegmentColorIndex, string.Empty));
        lSegmentList.Insert(lSegmentIndex, lSegment with { LSegmentEnd = lSegmentCursor });
        return (lSegmentList, lSegmentIndex, lSegmentIndex + 1);
    }
}
