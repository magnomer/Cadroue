using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LFlow TFlowCreate() => new();
    internal static void TFlowEditAttach(LFlow flow, Action<bool> handler) => flow.LFlowEditChange += handler;
    internal static void TFlowSourceSet(LFlow flow, LMediaInfo media, string? path) => flow.LFlowSourceSet(media, path);
    internal static bool TFlowSourceMatch(LFlow flow, string? path) => flow.LFlowSourceMatch(path);
    internal static void TFlowSourceClear(LFlow flow) => flow.LFlowSourceClear();
    internal static void TFlowCursorSet(LFlow flow, TimeSpan cursor) => flow.LFlowCursorSet(cursor);
    internal static void TFlowSectionSet(LFlow flow, bool active) => flow.LFlowSectionSet(active);
    internal static void TFlowCommandSet(LFlow flow, bool active) => flow.LFlowCommandSet(active);
    internal static void TFlowUnloadSet(LFlow flow) => flow.LFlowUnloadSet();
    internal static bool TFlowEditSet(LFlow flow, bool editable) => flow.LFlowEditSet(editable);
    internal static bool TFlowEditCheck(LFlow flow) => flow.LFlowEditCheck();
    internal static bool TFlowSourceCheck(LFlow flow) => flow.LFlowSourceCheck();
    internal static bool TFlowScanCheck(LFlow flow) => flow.LFlowScanCheck();
    internal static bool TFlowStampSet(LFlow flow, string stamp) => flow.LFlowStampSet(stamp);
    internal static bool TFlowLosslesscutSet(LFlow flow, string path) => flow.LFlowLosslesscutSet(path);
    internal static void TFlowAttach(LFlow flow, LMediaInfo media, string? path, TimeSpan cursor) =>
        flow.LFlowAttach(media, path, cursor);
    internal static bool TFlowClear(LFlow flow) => flow.LFlowClear();
    internal static void TFlowClose(LFlow flow) => flow.LFlowClose();
    internal static void TFlowPaletteApply(LFlow flow, int count) => flow.LFlowPaletteApply(count);
    internal static void TFlowPlayingAttach(LFlow flow, Func<bool>? source) => flow.LFlowPlayingAttach(source);
    internal static void TFlowSpoolAttach(LFlow flow, Action handler) => flow.LFlowSpoolApply += handler;
    internal static void TFlowCursorAttach(LFlow flow, Action apply, Action<TimeSpan> change)
    {
        flow.LFlowCursorApply += apply;
        flow.LFlowCursorChange += change;
    }
    internal static void TFlowStripAttach(LFlow flow, Action attach, Action clear)
    {
        flow.LFlowAttachApply += attach;
        flow.LFlowClearApply += clear;
    }
    internal static void TFlowMediaAttach(LFlow flow, Action handler) => flow.LFlowMediaChange += handler;
    internal static void TFlowPlayAttach(
        LFlow flow, Action play, Action pause, Action<bool> drag, Action<double> volume)
    {
        flow.LFlowPlay += play;
        flow.LFlowPause += pause;
        flow.LFlowDragChange += drag;
        flow.LFlowVolumeAdjust += volume;
    }
    internal static void TFlowNameAttach(LFlow flow, Action<LFlowNamePrompt> show, Action close)
    {
        flow.LFlowName.LFlowNameShow += show;
        flow.LFlowName.LFlowNameClose += close;
    }
    internal static bool TFlowShortcutRun(LFlow flow, string token) => flow.LFlowShortcutRun(token);
    internal static bool TFlowWheelHandle(LFlow flow, int delta) => flow.LFlowWheelHandle(delta);
    internal static void TFlowDragHandle(LFlow flow, bool dragging) => flow.LFlowDragHandle(dragging);
    internal static void TFlowCursorUpdate(LFlow flow, TimeSpan cursor) => flow.LFlowCursorUpdate(cursor);
    internal static void TFlowCursorSeek(LFlow flow, TimeSpan cursor) => flow.LFlowCursorSeek(cursor);
    internal static void TFlowRangeSet(LFlow flow, TimeSpan origin, TimeSpan limit) =>
        flow.LFlowRangeSet(origin, limit);
    internal static bool TFlowNameStart(LFlow flow) => flow.LFlowName.LFlowNameStart();
    internal static void TFlowNameHide(LFlow flow) => flow.LFlowName.LFlowNameHide();
    internal static void TFlowNameCommit(LFlow flow, string name, string prefix, string suffix) =>
        flow.LFlowName.LFlowNameCommit(name, prefix, suffix);
    internal static double TFlowOffsetResolve(bool empty, double origin, double size, double host) =>
        LFlowName.LFlowOffsetResolve(empty, origin, size, host);

    internal static void TFlowSectionAttach(LFlow flow, Action<IReadOnlyList<LPiece>, int?> handler) =>
        flow.LFlowSection.LFlowSectionChange += handler;
    internal static IReadOnlyList<LPiece> TFlowSectionsRead(LFlow flow) => flow.LFlowSection.LFlowSectionsRead();
    internal static int? TFlowSelectionRead(LFlow flow) => flow.LFlowSection.LFlowSelectionRead();
    internal static IReadOnlyList<int> TFlowSelectedRead(LFlow flow) => flow.LFlowSection.LFlowSelectedRead();
    internal static IReadOnlyList<LSplitSectionDescription> TFlowSplitRead(LFlow flow) =>
        flow.LFlowSection.LFlowSplitRead();
    internal static bool TFlowEmptyCheck(LFlow flow) => flow.LFlowSection.LFlowEmptyCheck();
    internal static void TFlowSectionAdd(LFlow flow) => flow.LFlowSection.LFlowSectionAdd();
    internal static void TFlowStartSet(LFlow flow) => flow.LFlowSection.LFlowStartSet();
    internal static void TFlowEndSet(LFlow flow) => flow.LFlowSection.LFlowEndSet();
    internal static void TFlowSectionDivide(LFlow flow) => flow.LFlowSection.LFlowSectionDivide();
    internal static void TFlowSectionDelete(LFlow flow) => flow.LFlowSection.LFlowSectionDelete();
    internal static void TFlowSectionClear(LFlow flow) => flow.LFlowSection.LFlowSectionClear();
    internal static void TFlowSectionSelect(LFlow flow, int index) => flow.LFlowSection.LFlowSectionSelect(index);
    internal static void TFlowSelectToggle(LFlow flow, int index) => flow.LFlowSection.LFlowSelectToggle(index);
    internal static void TFlowRangeSelect(LFlow flow, int index) => flow.LFlowSection.LFlowRangeSelect(index);
    internal static void TFlowSectionSeek(LFlow flow, int index, bool end) =>
        flow.LFlowSection.LFlowSectionSeek(index, end);
    internal static void TFlowSectionToggle(LFlow flow, int index) => flow.LFlowSection.LFlowSectionToggle(index);
    internal static bool TFlowSectionMove(LFlow flow, int source, int target) =>
        flow.LFlowSection.LFlowSectionMove(source, target);
    internal static bool TFlowSectionSort(LFlow flow) => flow.LFlowSection.LFlowSectionSort();
    internal static void TFlowNameSet(LFlow flow, int index, string name, string? prefix, string? suffix) =>
        flow.LFlowSection.LFlowNameSet(index, name, prefix, suffix);
    internal static bool TFlowCombineApply(
        LFlow flow,
        IReadOnlyList<LSweepSpan> excluded,
        IReadOnlyList<LSweepSpan> kept,
        IReadOnlyList<LSweepBoundary> boundaries) =>
        flow.LFlowSection.LFlowCombineApply(excluded, kept, boundaries);

    internal static void TFlowLosslesscutAttach(LFlow flow, Action<LFlowLosslesscutPrompt, Action<int>> handler) =>
        flow.LFlowLosslesscut.LFlowLosslesscutAsk += handler;
    internal static void TFlowLosslesscutFind(LFlow flow) => flow.LFlowLosslesscut.LFlowLosslesscutFind();
    internal static void TFlowLosslesscutRun(LFlow flow, string path) =>
        flow.LFlowLosslesscut.LFlowLosslesscutRun(path);
    internal static string TFlowLosslesscutFormat(string path, LLosslesscutResult result, bool range) =>
        LFlowLosslesscut.LFlowLosslesscutFormat(path, result, range);

    internal static void TFlowTimerAttach(LFlow flow, Action defer, Action<TimeSpan> resume, Action reset)
    {
        flow.LFlowKeyframe.LFlowTimerDefer += defer;
        flow.LFlowKeyframe.LFlowTimerResume += resume;
        flow.LFlowKeyframe.LFlowTimerReset += reset;
    }
    internal static void TFlowKeyframeAttach(
        LFlow flow,
        Action<LKeyframeNotice> ready,
        Action<IReadOnlyList<LKeyframeEntry>, IReadOnlyList<LKeyframeScanRange>> change)
    {
        flow.LFlowKeyframe.LFlowKeyframeReady += ready;
        flow.LFlowKeyframe.LFlowKeyframeChange += change;
    }
    internal static bool TFlowResumeCheck(LFlow flow) => flow.LFlowKeyframe.LFlowResumePending;
    internal static void TFlowKeyframeDefer(LFlow flow) => flow.LFlowKeyframe.LFlowKeyframeDefer();
    internal static void TFlowKeyframeSuspend(LFlow flow, bool resume) =>
        flow.LFlowKeyframe.LFlowKeyframeSuspend(resume);
    internal static void TFlowKeyframeReset(LFlow flow) => flow.LFlowKeyframe.LFlowKeyframeReset();
    internal static void TFlowKeyframeTick(LFlow flow) => flow.LFlowKeyframe.LFlowKeyframeTick();
    internal static void TFlowKeyframeMove(LFlow flow, int direction) =>
        flow.LFlowKeyframe.LFlowKeyframeMove(direction);
    internal static void TFlowKeyframeApply(LFlow flow, LKeyframeNotice notice) =>
        flow.LFlowKeyframe.LFlowKeyframeApply(notice);
    internal static LKeyframeNotice TKeyframeNoticeCreate(
        int serial, IReadOnlyList<LKeyframeEntry> list, IReadOnlyList<LKeyframeScanRange> ranges, LKeyframeKind kind) =>
        new(serial, list, ranges, kind);

    internal static void TFlowWaveformAttach(LFlow flow, Action ready, Action<byte[]> update)
    {
        flow.LFlowWaveform.LFlowWaveformReady += ready;
        flow.LFlowWaveform.LFlowWaveformUpdate += update;
    }
    internal static bool TFlowWaveformSet(LFlow flow, bool active) => flow.LFlowWaveformSet(active);
    internal static void TFlowWaveformApply(LFlow flow) => flow.LFlowWaveform.LFlowWaveformApply();
    internal static byte[] TFlowPeaksRead(LFlow flow) => flow.LFlowWaveform.LFlowWaveformPeaks;
    internal static LFlowWaveformOutline TFlowOutlineResolve(
        byte[] peaks, double width, double top, double height, TimeSpan start, TimeSpan end) =>
        LFlowWaveform.LFlowOutlineResolve(peaks, width, top, height, start, end);

    internal static LDivider TDividerCreate() => new();
    internal static void TDividerPressHandle(LDivider divider, double y, double height) =>
        divider.LDividerPressHandle(y, height);
    internal static double TDividerMoveResolve(LDivider divider, double y, double height) =>
        divider.LDividerMoveResolve(y, height);
    internal static void TDividerRelease(LDivider divider) => divider.LDividerRelease();

    internal static string TCursorTimeFormat(TimeSpan time) => LCursor.LCursorTimeFormat(time);
    internal static int TCursorWheelResolve(int delta) => LCursor.LCursorWheelResolve(delta);
    internal static (double, double) TCursorGuideResolve(double x, double thickness) =>
        LCursor.LCursorGuideResolve(x, thickness);
    internal static (double, double) TCursorChipResolve(
        double x, double chipWidth, double chipHeight, double top, double bottom, double width) =>
        LCursor.LCursorChipResolve(x, chipWidth, chipHeight, top, bottom, width);
    internal static IReadOnlyList<LCursorLine> TCursorLinesResolve(
        double top, double bottom, bool empty, double chipTop, double chipBottom, double chipHeight) =>
        LCursor.LCursorLinesResolve(top, bottom, empty, chipTop, chipBottom, chipHeight);

    internal static void TTraceAttach(Action<LTraceEntry> handler) => LTrace.LTraceAppend += handler;
    internal static void TTraceDetach(Action<LTraceEntry> handler) => LTrace.LTraceAppend -= handler;
}
