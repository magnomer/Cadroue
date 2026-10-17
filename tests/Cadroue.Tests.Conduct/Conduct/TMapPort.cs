using System;
using System.Collections.Generic;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// A map port over the flow's own range: it reads and edits the range as the outlet does, answers the sections and
// scans a test sets, and keeps every call and every drawn map.
internal sealed class TMapPort : LMapPort
{
    public List<string> TMapCalls { get; } = [];

    public IReadOnlyList<(TimeSpan, TimeSpan, int, bool)> TMapSections { get; set; } = [];

    public IReadOnlyList<(TimeSpan, TimeSpan)> TMapCoverage { get; set; } = [];

    public List<(TimeSpan, string?, string, double, int)> TMapRecords { get; } = [];

    public (TimeSpan LMapOrigin, TimeSpan LMapLimit, TimeSpan LMapDuration) LMapRangeRead(LSpool lRange) =>
        (lRange.LSpoolRangeOrigin, lRange.LSpoolRangeLimit, lRange.LSpoolDuration);

    public double LMapRatioResolve(LSpool lRange, TimeSpan lTime) => lRange.LSpoolRatioResolve(lTime);

    public TimeSpan LMapTimeResolve(LSpool lRange, double lRatio) => lRange.LSpoolTimeResolve(lRatio);

    public void LMapOriginSet(LSpool lRange, TimeSpan lOrigin)
    {
        TMapCalls.Add($"origin {lOrigin}");
        lRange.LSpoolStartSet(lOrigin);
    }

    public void LMapLimitSet(LSpool lRange, TimeSpan lLimit)
    {
        TMapCalls.Add($"limit {lLimit}");
        lRange.LSpoolEndSet(lLimit);
    }

    public void LMapBodyMove(LSpool lRange, TimeSpan lStep)
    {
        TMapCalls.Add($"move {lStep}");
        lRange.LSpoolMove(lStep);
    }

    public IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapEnd, int LMapColor, bool LMapHidden)> LMapSectionsRead(
        LSegment lSegment) => TMapSections;

    public IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapLimit)> LMapCoverageRead(
        IReadOnlyList<LKeyframeScanRange> lRanges, LSpool lRange) => TMapCoverage;

    public void LMapDrawRecord(TimeSpan lCursor, string? lSource, string lTrigger, double lMilliseconds, int lGlyphs) =>
        TMapRecords.Add((lCursor, lSource, lTrigger, lMilliseconds, lGlyphs));
}
