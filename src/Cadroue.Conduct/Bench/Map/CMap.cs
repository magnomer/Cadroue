using System;
using System.Collections.Generic;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One section's band on the map rail, in pixels, with its colour, its number and whether it shows and is selected.
public sealed record CMapBand(
    double CMapBandLeft,
    double CMapBandTop,
    double CMapBandRight,
    double CMapBandBottom,
    int CMapBandColor,
    string CMapBandBadge,
    bool CMapBandShown,
    bool CMapBandSelected);

// The map's answer for one size, in pixels. Sized: the background shows. Railed: a range is held and the rail fits,
// so the rail, its outline, the bands, the coverage strip (top and bottom) and its scans (left and width) show.
// Body: the navigator over the range, as
// left, width and rail height, with the cursor beside it; null while the range has no width.
public sealed record CMapFrame(
    bool CMapFrameSized,
    bool CMapFrameRailed,
    double CMapFrameBottom,
    (double, double) CMapFrameCoverage,
    bool CMapFrameWaveform,
    (double, double, IReadOnlyList<(double, double)>) CMapFrameOutline,
    IReadOnlyList<CMapBand> CMapFrameBands,
    IReadOnlyList<(double, double)> CMapFrameScans,
    (double, double, double)? CMapFrameBody,
    IReadOnlyList<double> CMapFrameCursor);

// The map's draw: the frame it paints for a size, the rail's geometry, what asked for the last draw and how many bands
// it drew. It makes the pointer's drag. The driver hands it the surface's size and paints its answers.
public sealed class CMap
{
    public const double CMapRailTop = 3;
    public const double CMapHandleWidth = 12;
    public const double CMapCoverageHeight = 3;
    private const double CMapRenderLeast = 12;
    private const double CMapSectionInset = 1;

    private static readonly (double, double, IReadOnlyList<(double, double)>) _cMapOutlineEmpty =
        (0, 0, Array.Empty<(double, double)>());

    private readonly LMapPort _cMapPort;
    private readonly CFlow _cMapFlow;
    private string _cMapTrigger = "attach";
    private int _cMapGlyphCount;

    internal CMap(LMapPort port, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(flow);
        _cMapPort = port;
        _cMapFlow = flow;
        CMapDrag = new CMapDrag(port, this, flow);
    }

    public CMapDrag CMapDrag { get; }

    public string CMapTrigger => _cMapTrigger;

    public int CMapGlyphCount => _cMapGlyphCount;

    // The grab zone in the middle of the navigator body, as left, width and height.
    public static (double, double, double) CMapMoveResolve(
        double bodyLeft, double bodyWidth, double bodyHeight, double sideWidth, double width)
    {
        double moveHeight = Math.Clamp(bodyHeight, 1, 16);
        double spaceWidth = Math.Max(0, bodyWidth - sideWidth * 2);
        if (spaceWidth <= 0)
        {
            return (0, 0, 0);
        }

        double moveWidth = Math.Clamp(bodyWidth * 0.42, Math.Min(72, spaceWidth), Math.Min(320, spaceWidth));
        double centered = bodyLeft + (bodyWidth - moveWidth) / 2;
        double leftMinimum = bodyLeft + sideWidth;
        double leftMaximum = bodyLeft + bodyWidth - sideWidth - moveWidth;
        double moveLeft = leftMaximum < leftMinimum ? centered : Math.Clamp(centered, leftMinimum, leftMaximum);
        return (Math.Clamp(moveLeft, 0, Math.Max(0, width - moveWidth)), moveWidth, moveHeight);
    }

    public void CMapTriggerSet(string trigger) => _cMapTrigger = trigger;

    // One draw: the frame for this size goes to the paint, then the draw and its time go on the trace timeline.
    public void CMapDraw(double width, double height, Action<CMapFrame> paint, Func<double> elapsed)
    {
        ArgumentNullException.ThrowIfNull(paint);
        ArgumentNullException.ThrowIfNull(elapsed);
        paint(CMapFrameResolve(width, height));
        _cMapPort.LMapDrawRecord(
            _cMapFlow.CFlowCursor, _cMapFlow.CFlowSource, _cMapTrigger, elapsed(), _cMapGlyphCount);
    }

    // The rail over a height: the coverage strip's top below it, and the rail's bottom.
    internal static (double, double) CMapRailResolve(double height)
    {
        double coverageTop = Math.Max(0, Math.Max(0, height - 1) - CMapCoverageHeight);
        return (coverageTop, Math.Max(CMapRailTop, coverageTop - 2));
    }

    // Where a time sits on a rail of this width.
    internal double CMapSpoolResolve(LSpool range, TimeSpan time, double width) =>
        Math.Clamp(_cMapPort.LMapRatioResolve(range, time), 0, 1) * width;

    private CMapFrame CMapFrameResolve(double width, double height)
    {
        _cMapGlyphCount = 0;
        var frame = new CMapFrame(
            width > 0 && height > 0, false, 0, (0, 0), false, _cMapOutlineEmpty, [], [], null, []);
        if (!frame.CMapFrameSized
            || _cMapFlow.CFlowRange is not { } range
            || _cMapPort.LMapRangeRead(range).LMapDuration <= TimeSpan.Zero
            || height < CMapRenderLeast)
        {
            return frame;
        }

        (double coverageTop, double railBottom) = CMapRailResolve(height);
        double railHeight = Math.Max(0, railBottom - CMapRailTop);
        if (railHeight <= 0)
        {
            return frame;
        }

        CMapFrame railed = CMapBaseResolve(frame, range, width, railBottom, railHeight) with
        {
            CMapFrameCoverage = (coverageTop, coverageTop + CMapCoverageHeight),
        };
        _cMapGlyphCount = railed.CMapFrameBands.Count;
        return CMapOverResolve(railed, range, width, coverageTop, railHeight);
    }

    private CMapFrame CMapBaseResolve(CMapFrame frame, LSpool range, double width, double railBottom, double railHeight)
    {
        TimeSpan duration = _cMapPort.LMapRangeRead(range).LMapDuration;
        return frame with
        {
            CMapFrameRailed = true,
            CMapFrameBottom = railBottom,
            CMapFrameWaveform = _cMapFlow.CFlowWaveform.CWaveformShown,
            CMapFrameOutline = _cMapFlow.CFlowWaveform.CWaveformOutlineResolve(
                width, CMapRailTop, railHeight, TimeSpan.Zero, duration),
            CMapFrameBands = CMapBandsResolve(duration, width, railHeight),
        };
    }

    private CMapFrame CMapOverResolve(
        CMapFrame frame, LSpool range, double width, double coverageTop, double railHeight)
    {
        (TimeSpan origin, TimeSpan limit, TimeSpan duration) = _cMapPort.LMapRangeRead(range);
        IReadOnlyList<(double, double)> scans = CMapScansResolve(range, duration, width);
        double bodyLeft = CMapSpoolResolve(range, origin, width);
        double bodyRight = CMapSpoolResolve(range, limit, width);
        double bodyWidth = Math.Max(0, Math.Max(bodyLeft, bodyRight) - Math.Min(bodyLeft, bodyRight));
        if (bodyWidth <= 0)
        {
            return frame with { CMapFrameScans = scans };
        }

        return frame with
        {
            CMapFrameScans = scans,
            CMapFrameBody = (Math.Min(bodyLeft, bodyRight), bodyWidth, railHeight),
            CMapFrameCursor = [CMapSpoolResolve(range, _cMapFlow.CFlowCursor, width)],
        };
    }

    private IReadOnlyList<CMapBand> CMapBandsResolve(TimeSpan duration, double width, double railHeight)
    {
        var sections = _cMapPort.LMapSectionsRead(_cMapFlow.CFlowSection.CSectionSegment);
        int? selection = _cMapFlow.CFlowSection.CSectionSelection;
        double durationSeconds = duration.TotalSeconds;
        var bands = new List<CMapBand>(sections.Count);
        double top = CMapRailTop + CMapSectionInset;
        double height = Math.Max(4, railHeight - CMapSectionInset * 2);
        for (int index = 0; index < sections.Count; index++)
        {
            (TimeSpan origin, TimeSpan end, int color, bool hidden) = sections[index];
            if (end <= origin)
            {
                continue;
            }

            double left = Math.Clamp(origin.TotalSeconds / durationSeconds * width, 0, width);
            double right = Math.Clamp(end.TotalSeconds / durationSeconds * width, 0, width);
            bands.Add(new CMapBand(
                left, top, left + Math.Max(1, right - left), top + height, color, $"{index + 1}", !hidden,
                index == selection));
        }

        return bands;
    }

    // Each scanned span as left and width on the coverage strip; a span with no width left is skipped.
    private IReadOnlyList<(double, double)> CMapScansResolve(LSpool range, TimeSpan duration, double width)
    {
        double durationSeconds = duration.TotalSeconds;
        var scans = new List<(double, double)>();
        foreach ((TimeSpan origin, TimeSpan limit) in
            _cMapPort.LMapCoverageRead(_cMapFlow.CFlowKeyframe.CKeyframeRanges, range))
        {
            double left = Math.Clamp(origin.TotalSeconds / durationSeconds * width, 0, width);
            double right = Math.Clamp(limit.TotalSeconds / durationSeconds * width, 0, width);
            double scanWidth = Math.Min(Math.Max(1, right - left), Math.Max(0, width - left));
            if (scanWidth > 0)
            {
                scans.Add((left, scanWidth));
            }
        }

        return scans;
    }
}
