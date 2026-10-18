using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static QFlow TFlowCreate()
    {
        Cadroue.Conduct.CAtelier atelier = TAtelierCreate();
        return new(atelier.CAtelierUsher, atelier.CAtelierWorkspaceCreate().CWorkspaceFlow);
    }

    internal static void TFlowEditAttach(QFlow flow, Action<bool> handler) =>
        flow.QFlowEdit.QFlowEditChanged += handler;
    internal static void TFlowSourceSet(QFlow flow, LMediaInfo media, string? path) => flow.QFlowSourceSet(media, path);
    internal static bool TFlowSourceMatch(QFlow flow, string? path) => flow.QFlowSourceMatch(path);
    internal static void TFlowSourceClear(QFlow flow) => flow.QFlowSourceClear();
    internal static void TFlowCursorSet(QFlow flow, TimeSpan cursor) => flow.QFlowCursorSet(cursor);
    internal static void TFlowSectionSet(QFlow flow, bool active) => flow.QFlowSectionSet(active);
    internal static void TFlowCommandSet(QFlow flow, bool active) => flow.QFlowCommandSet(active);
    internal static void TFlowUnloadSet(QFlow flow) => flow.QFlowUnloadSet();
    internal static bool TFlowEditSet(QFlow flow, bool editable) => flow.QFlowEdit.QFlowEditSet(editable);
    internal static bool TFlowEditCheck(QFlow flow) => flow.QFlowEdit.QFlowEditCheck();
    internal static bool TFlowSourceCheck(QFlow flow) => flow.QFlowSourceCheck();
    internal static bool TFlowScanCheck(QFlow flow) => flow.QFlowScanCheck();
    internal static bool TFlowLosslesscutSet(QFlow flow, string path) => flow.QFlowStamp.QFlowLosslesscutSet(path);
    internal static void TFlowAttach(QFlow flow, LMediaInfo media, string? path, TimeSpan cursor) =>
        flow.QFlowAttach(media, path, cursor);
    internal static bool TFlowClear(QFlow flow) => flow.QFlowClear();
    internal static void TFlowClose(QFlow flow) => flow.QFlowClose();
    internal static void TFlowPaletteApply(QFlow flow, int count) => flow.QFlowPaletteApply(count);
    internal static void TFlowPlayingAttach(QFlow flow, Func<bool>? source) =>
        flow.QFlowWheel.QFlowPlayingAttach(source);
    internal static void TFlowSpoolAttach(QFlow flow, Action handler) => flow.QFlowSpool.QFlowSpoolApplied += handler;
    internal static void TFlowCursorAttach(QFlow flow, Action apply, Action<TimeSpan> change)
    {
        flow.QFlowCursorApplied += apply;
        flow.QFlowCursorChanged += change;
    }
    internal static void TFlowStripAttach(QFlow flow, Action attach, Action clear)
    {
        flow.QFlowAttachApplied += attach;
        flow.QFlowClearApplied += clear;
    }
    internal static void TFlowMediaAttach(QFlow flow, Action handler) => flow.QFlowMediaChanged += handler;
    internal static void TFlowPlayAttach(
        QFlow flow, Action play, Action pause, Action<bool> drag, Action<double> volume)
    {
        flow.QFlowPlayed += play;
        flow.QFlowPaused += pause;
        flow.QFlowWheel.QFlowDragChanged += drag;
        flow.QFlowWheel.QFlowVolumeAdjusted += volume;
    }
    internal static void TFlowNameAttach(QFlow flow, Action<QFlowNamePrompt> show, Action close)
    {
        flow.QFlowName.QFlowNameShown += show;
        flow.QFlowName.QFlowNameClosed += close;
    }
    internal static bool TFlowShortcutRun(QFlow flow, string token) => flow.QFlowShortcutRun(token);
    internal static bool TFlowWheelHandle(QFlow flow, int delta) => flow.QFlowWheel.QFlowWheelHandle(delta);
    internal static void TFlowDragHandle(QFlow flow, bool dragging) => flow.QFlowWheel.QFlowDragHandle(dragging);
    internal static void TFlowCursorUpdate(QFlow flow, TimeSpan cursor) => flow.QFlowCursorUpdate(cursor);
    internal static void TFlowCursorSeek(QFlow flow, TimeSpan cursor) => flow.QFlowCursorSeek(cursor);
    internal static void TFlowRangeSet(QFlow flow, TimeSpan origin, TimeSpan limit) =>
        flow.QFlowSpool.QFlowRangeSet(origin, limit);
    internal static bool TFlowNameStart(QFlow flow) => flow.QFlowName.QFlowNameStart();
    internal static void TFlowNameHide(QFlow flow) => flow.QFlowName.QFlowNameHide();
    internal static void TFlowNameCommit(QFlow flow, string name, string prefix, string suffix) =>
        flow.QFlowName.QFlowNameCommit(name, prefix, suffix);
    internal static double TFlowOffsetResolve(bool empty, double origin, double size, double host) =>
        QFlowName.QFlowOffsetResolve(empty, origin, size, host);

    internal static void TFlowSectionAttach(QFlow flow, Action<IReadOnlyList<LPiece>, int?> handler) =>
        flow.QFlowSection.QFlowSectionChanged += handler;
    internal static IReadOnlyList<LPiece> TFlowSectionsRead(QFlow flow) => flow.QFlowSection.QFlowSectionsRead();
    internal static int? TFlowSelectionRead(QFlow flow) => flow.QFlowSection.QFlowSelectionRead();
    internal static IReadOnlyList<int> TFlowSelectedRead(QFlow flow) => flow.QFlowSection.QFlowSelectedRead();
    internal static IReadOnlyList<LSplitSectionDescription> TFlowSplitRead(QFlow flow) =>
        flow.QFlowSection.QFlowSplitRead();
    internal static bool TFlowEmptyCheck(QFlow flow) => flow.QFlowSection.QFlowEmptyCheck();
    internal static void TFlowSectionAdd(QFlow flow) => flow.QFlowSection.QFlowSectionAdd();
    internal static void TFlowStartSet(QFlow flow) => flow.QFlowSection.QFlowStartSet();
    internal static void TFlowEndSet(QFlow flow) => flow.QFlowSection.QFlowEndSet();
    internal static void TFlowSectionDivide(QFlow flow) => flow.QFlowSection.QFlowSectionDivide();
    internal static void TFlowSectionDelete(QFlow flow) => flow.QFlowSection.QFlowSectionDelete();
    internal static void TFlowSectionClear(QFlow flow) => flow.QFlowSection.QFlowSectionClear();
    internal static void TFlowSectionSelect(QFlow flow, int index) => flow.QFlowSection.QFlowSectionSelect(index);
    internal static void TFlowSelectToggle(QFlow flow, int index) => flow.QFlowSection.QFlowSelectToggle(index);
    internal static void TFlowRangeSelect(QFlow flow, int index) => flow.QFlowSection.QFlowRangeSelect(index);
    internal static void TFlowSectionSeek(QFlow flow, int index, bool end) =>
        flow.QFlowSection.QFlowSectionSeek(index, end);
    internal static void TFlowSectionToggle(QFlow flow, int index) => flow.QFlowSection.QFlowSectionToggle(index);
    internal static bool TFlowSectionMove(QFlow flow, int source, int target) =>
        flow.QFlowSection.QFlowSectionMove(source, target);
    internal static bool TFlowSectionSort(QFlow flow) => flow.QFlowSection.QFlowSectionSort();
    internal static void TFlowNameSet(QFlow flow, int index, string name, string? prefix, string? suffix) =>
        flow.QFlowSection.QFlowNameSet(index, name, prefix, suffix);
    internal static bool TFlowCombineApply(
        QFlow flow,
        IReadOnlyList<LSweepSpan> excluded,
        IReadOnlyList<LSweepSpan> kept,
        IReadOnlyList<LSweepBoundary> boundaries) =>
        flow.QFlowSection.QFlowCombineApply(excluded, kept, boundaries);

    internal static void TFlowLosslesscutAttach(QFlow flow, Action<QFlowLosslesscutPrompt, Action<int>> handler) =>
        flow.QFlowLosslesscut.QFlowLosslesscutAsk += handler;
    internal static void TFlowLosslesscutFind(QFlow flow) => flow.QFlowLosslesscut.QFlowLosslesscutFind();
    internal static void TFlowLosslesscutRun(QFlow flow, string path) =>
        flow.QFlowLosslesscut.QFlowLosslesscutRun(path);
    internal static string TFlowLosslesscutFormat(string path, LLosslesscutResult result, bool range) =>
        QFlowLosslesscut.QFlowLosslesscutFormat(TAtelierCreate().CAtelierUsher, path, result, range);

    internal static void TFlowTimerAttach(QFlow flow, Action defer, Action<TimeSpan> resume, Action reset)
    {
        flow.QFlowKeyframe.QFlowTimerDeferred += defer;
        flow.QFlowKeyframe.QFlowTimerResumed += resume;
        flow.QFlowKeyframe.QFlowTimerStopped += reset;
    }
    internal static bool TFlowResumeCheck(QFlow flow) => flow.QFlowKeyframe.QFlowResumePending;
    internal static void TFlowKeyframeDefer(QFlow flow) => flow.QFlowKeyframe.QFlowKeyframeDefer();
    internal static void TFlowKeyframeSuspend(QFlow flow, bool resume) =>
        flow.QFlowKeyframe.QFlowKeyframeSuspend(resume);
    internal static void TFlowKeyframeTick(QFlow flow) => flow.QFlowKeyframe.QFlowKeyframeTick();

    internal static void TFlowWaveformAttach(QFlow flow, Action ready, Action update)
    {
        flow.QFlowWaveform.QFlowWaveformReady += ready;
        flow.QFlowWaveform.QFlowWaveformUpdated += update;
    }
    internal static bool TFlowWaveformSet(QFlow flow, bool active) => flow.QFlowWaveformSet(active);
    internal static void TFlowWaveformApply(QFlow flow) => flow.QFlowWaveform.QFlowWaveformApply();
    internal static bool TFlowWaveformCheck(QFlow flow) => flow.QFlowWaveform.QFlowWaveformGate.CWaveformShown;

    internal static QDivider TDividerCreate() => new();
    internal static void TDividerPressHandle(QDivider divider, double y, double height) =>
        divider.QDividerPressHandle(y, height);
    internal static double TDividerMoveResolve(QDivider divider, double y, double height) =>
        divider.QDividerMoveResolve(y, height);
    internal static void TDividerRelease(QDivider divider) => divider.QDividerRelease();

    internal static string TCursorTimeFormat(TimeSpan time) => QCursor.QCursorTimeFormat(time);
    internal static int TCursorWheelResolve(int delta) => QCursor.QCursorWheelResolve(delta);
    internal static (double, double) TCursorGuideResolve(double x, double thickness) =>
        QCursor.QCursorGuideResolve(x, thickness);
    internal static (double, double) TCursorChipResolve(
        double x, double chipWidth, double chipHeight, double top, double bottom, double width) =>
        QCursor.QCursorChipResolve(x, chipWidth, chipHeight, top, bottom, width);
    internal static IReadOnlyList<QCursorLine> TCursorLinesResolve(
        double top, double bottom, bool empty, double chipTop, double chipBottom, double chipHeight) =>
        QCursor.QCursorLinesResolve(top, bottom, empty, chipTop, chipBottom, chipHeight);

    internal static QMap TMapRead(QFlow flow) => flow.QFlowMap;
    internal static void TMapFrameAttach(QMap map, Action handler) => map.QMapFrameApplied += handler;
    internal static QMapFrame TMapFrameResolve(QMap map, double width, double height)
    {
        QMapFrame? frame = null;
        map.QMapDraw(width, height, painted => frame = painted);
        return frame!;
    }

    internal static Cadroue.Conduct.CMapHit TMapDragRead(QMap map) => map.QMapGate.CMapDrag.CMapDragKind;
    internal static string TMapTriggerRead(QMap map) => map.QMapGate.CMapTrigger;
    internal static int TMapGlyphRead(QMap map) => map.QMapGate.CMapGlyphCount;
    internal static IReadOnlyList<QMapBadge> TMapBadgeResolve(QMap map, QMapBand band, double width, double height) =>
        QMapText.QMapBadgeResolve(band, width, height);
    internal static QMapHitKind TMapHoverResolve(QMap map, double x, double y, double width, double height) =>
        map.QMapHoverResolve(x, y, width, height);
    internal static QMapHitKind TMapLeaveResolve(QMap map) => map.QMapLeaveResolve();
    internal static bool TMapPressHandle(QMap map, double x, double y, double width, double height) =>
        map.QMapPressHandle(x, y, width, height);
    internal static bool TMapMoveHandle(QMap map, double x, double width) => map.QMapMoveHandle(x, width);
    internal static void TMapDragClear(QMap map) => map.QMapDragClear();

    internal static QViewfinder TViewfinderRead(QFlow flow) => flow.QFlowViewfinder;
    internal static void TViewfinderFrameAttach(QViewfinder viewfinder, Action handler) =>
        viewfinder.QViewfinderFrameApplied += handler;
    internal static QViewfinderFrame TViewfinderFrameResolve(QViewfinder viewfinder, double width, double height) =>
        viewfinder.QViewfinderFrameResolve(width, height);
    internal static QViewfinderPoint TViewfinderTickResolve(QViewfinderGraduation tick, double textHeight) =>
        QViewfinderText.QViewfinderTickResolve(tick, textHeight);
    internal static IReadOnlyList<QViewfinderLabel> TViewfinderLabelResolve(
        QViewfinderBand band, double badgeWidth, double badgeHeight) =>
        QViewfinderText.QViewfinderLabelResolve(band, badgeWidth, badgeHeight);
    internal static QViewfinderBadge TViewfinderBadgeResolve(
        QViewfinderBand band,
        QViewfinderLabel label,
        double badgeWidth,
        double badgeHeight,
        double nameWidth,
        double nameHeight) =>
        QViewfinderText.QViewfinderBadgeResolve(band, label, badgeWidth, badgeHeight, nameWidth, nameHeight);
    internal static QViewfinderChip TViewfinderChipResolve(
        QViewfinderCursor cursor, double textWidth, double textHeight, double width, double height) =>
        QViewfinderText.QViewfinderChipResolve(cursor, textWidth, textHeight, width, height);
    internal static QViewfinderPoint TViewfinderPopupResolve(
        QViewfinder viewfinder, int index, double width, double height) =>
        viewfinder.QViewfinderPopupResolve(index, width, height);
    internal static bool TViewfinderPressHandle(QViewfinder viewfinder, double x, double width) =>
        viewfinder.QViewfinderPressHandle(x, width);
    internal static bool TViewfinderMoveHandle(QViewfinder viewfinder, double x, double width) =>
        viewfinder.QViewfinderMoveHandle(x, width);
    internal static void TViewfinderDragClear(QViewfinder viewfinder) => viewfinder.QViewfinderDragClear();

    internal static void TTraceAttach(Action<LTraceEntry> handler) => LTrace.LTraceAppended += handler;

    internal static void TTraceDetach(Action<LTraceEntry> handler) => LTrace.LTraceAppended -= handler;
}
