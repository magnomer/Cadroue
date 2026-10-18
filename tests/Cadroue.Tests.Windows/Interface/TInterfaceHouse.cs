using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static QWindow TWindowCreate(Func<Func<bool>, bool> dispatch)
    {
        QWindow window = new(TAtelierCreate());
        window.QWindowDispatcherAttach(dispatch);
        return window;
    }
    internal static QStrip TWindowStripRead(QWindow window) => window.QWindowStrip;
    internal static void TWindowTabSet(QWindow window, QStripTab? tab, QViewer? viewer) =>
        window.QWindowTabSet(tab, viewer);
    internal static void TWindowAddAttach(QWindow window, Action<QWindowTab> handler) =>
        window.QWindowTabAdded += handler;
    internal static void TWindowSelectAttach(QWindow window, Action<QStripTab, double> attach, Action<QStripTab> detach)
    {
        window.QWindowTabAttached += attach;
        window.QWindowTabDetached += detach;
    }
    internal static LSceneDocument TWindowSceneRead(QWindow window, string name) =>
        window.QWindowScene.QWindowSceneRead(name);
    internal static (double, double) TWindowWidthResolve(
        QWindow window, double? required, double reserved, double width) =>
        window.QWindowWidthResolve(required, reserved, width);
    internal static void TStripWorkspaceAttach(QStripTab tab, LPreset preset)
    {
        tab.QStripTabWorkspace.QWorkspacePresetAccept(preset);
        tab.QStripTabWorkspace.QWorkspaceAttach(() => new LSceneTabEntry());
    }
    internal static LPreset TPresetInitialCreate(string key) => LPresetTable.LPresetInitialCreate(key);

    internal static QWindowDrag TWindowDragCreate(
        string[]? paths, bool group, string? source, bool copy, object? data) =>
        new(paths, group, source, copy, data);
    internal static void TWindowOverHandle(QWindow window, QWindowDrag drag) =>
        window.QWindowPart.QWindowPartDrop.QWindowOverHandle(drag);
    internal static void TWindowDropHandle(QWindow window, QWindowDrag drag) =>
        window.QWindowPart.QWindowPartDrop.QWindowDropHandle(drag);
    internal static QWindowDropEffect TWindowEffectRead(QWindow window) =>
        window.QWindowPart.QWindowPartDrop.QWindowDropEffect;
    internal static bool TWindowHandledRead(QWindow window) => window.QWindowPart.QWindowPartDrop.QWindowDropHandled;
    internal static void TWindowDropAttach(QWindow window, Action<IReadOnlyList<string>> list, Action<string> viewer)
    {
        window.QWindowPart.QWindowPartDrop.QWindowListAdded += list;
        window.QWindowPart.QWindowPartDrop.QWindowViewerOpened += viewer;
    }

    internal static void TWindowShortcutAttach(
        QWindow window,
        Action show,
        Func<bool?> undo,
        Func<bool?> redo,
        Func<bool> unload,
        Func<IReadOnlySet<Guid>, bool?> clear) =>
        window.QWindowPart.QWindowPartShortcut.QWindowShortcutAttach(show, undo, redo, unload, clear);
    internal static bool TWindowShortcutRun(QWindow window, string token) =>
        window.QWindowPart.QWindowPartShortcut.QWindowShortcutRun(token);
    internal static void TWindowPlayAttach(QWindow window, Action play, Action pause)
    {
        window.QWindowPart.QWindowPartShortcut.QWindowShortcutPlayed += play;
        window.QWindowPart.QWindowPartShortcut.QWindowShortcutPaused += pause;
    }

    internal static QWindowPress TWindowPressCreate(
        bool handled,
        int message,
        int virtualKey,
        bool modal,
        nint own,
        Func<nint> foreground,
        Func<nint, bool?> viewer,
        Func<bool> input,
        Func<int, string> key,
        Func<int, short> state) =>
        new(handled, message, virtualKey, modal, own, foreground, viewer, input, key, state);
    internal static bool TWindowShortcutHandle(QWindow window, QWindowPress press) =>
        window.QWindowPart.QWindowPartShortcut.QWindowShortcutHandle(press);

    internal static QSash TSashCreate(double border) => new(border);
    internal static QSashBounds TSashBoundsCreate(double left, double top, double width, double height) =>
        new(left, top, width, height);
    internal static int TSashDirectionResolve(double x, double y, double width, double height, double border) =>
        QSash.QSashDirectionResolve(x, y, width, height, border);
    internal static bool TSashPressHandle(
        QSash sash,
        bool normal,
        bool interactive,
        double x,
        double y,
        double pointerX,
        double pointerY,
        QSashBounds bounds) =>
        sash.QSashPressHandle(normal, interactive, x, y, pointerX, pointerY, bounds);
    internal static bool TSashMoveHandle(
        QSash sash,
        bool normal,
        bool interactive,
        double x,
        double y,
        double width,
        double height,
        double pointerX,
        double pointerY,
        double minimumWidth,
        double minimumHeight) =>
        sash.QSashMoveHandle(normal, interactive, x, y, width, height, pointerX, pointerY, minimumWidth, minimumHeight);
    internal static bool TSashReleaseHandle(QSash sash) => sash.QSashReleaseHandle();
    internal static void TSashCaptureHandle(QSash sash) => sash.QSashCaptureHandle();
    internal static int TSashLeaveResolve(QSash sash) => sash.QSashLeaveResolve();
    internal static void TSashBoundsAttach(QSash sash, Action<QSashBounds> handler) =>
        sash.QSashBoundsChanged += handler;
    internal static void TSashCaptureAttach(QSash sash, Action start, Action stop, Action<bool> active)
    {
        sash.QSashCaptureStarted += start;
        sash.QSashCaptureStopped += stop;
        sash.QSashActiveChanged += active;
    }

    internal static QSashBounds TSashBoundsClamp(
        QSashBounds bounds,
        QSashBounds work,
        double minWidth,
        double minHeight,
        double maxWidth,
        double maxHeight) =>
        QSashBounds.QSashBoundsClamp(bounds, work, minWidth, minHeight, maxWidth, maxHeight);
    internal static QSashBounds TSashPlacementResolve(
        double? left,
        double? top,
        double width,
        double height,
        QSashBounds current,
        double minWidth,
        double minHeight,
        double workLeft,
        double workTop) =>
        QSashPlacement.QSashPlacementResolve(left, top, width, height, current, minWidth, minHeight, workLeft, workTop);
    internal static (double, double) TSashRelayResolve(
        double dropLeft,
        double dropTop,
        double screenLeft,
        double screenTop,
        double screenWidth,
        double screenHeight) =>
        QSashRelay.QSashRelayResolve(dropLeft, dropTop, screenLeft, screenTop, screenWidth, screenHeight);
    internal static (double, double) TSashCenterResolve(bool manual, QSashBounds window, QSashBounds owner) =>
        QSashCenter.QSashCenterResolve(manual, window, owner);

    internal static IReadOnlyList<QTokenPart> TTokenParse(string text) => QToken.QTokenParse(text);
    internal static QTokenPart TTokenPartResolve(string token) => QToken.QTokenPartResolve(token);
    internal static string TTokenTextRead(IEnumerable<(string?, string?)> inlines) => QToken.QTokenTextRead(inlines);
    internal static QToken TTokenCreate() => new();
    internal static bool TTokenDropHandle(QToken token, string? text) => token.QTokenDropHandle(text);
    internal static void TTokenInsertAttach(QToken token, Action<string> handler) => token.QTokenInserted += handler;

    internal static QGhost TGhostCreate(double grabX, double grabY) => new(grabX, grabY);
    internal static void TGhostPointSet(QGhost ghost, double x, double y) => ghost.QGhostPointSet(x, y);
    internal static bool TGhostClear(QGhost ghost) => ghost.QGhostClear();
    internal static QGhostSize TGhostSizeResolve(double width, double height, double scaleX, double scaleY) =>
        QGhost.QGhostSizeResolve(width, height, scaleX, scaleY);

    internal static IReadOnlyList<QPickerItem> TPickerItemsCreate(
        IReadOnlyList<string> tokens, IReadOnlyList<string> selected) =>
        QPicker.QPickerItemsCreate([.. tokens.Select(token => new LLocalizationChoice(token))], selected);
    internal static (string, bool) TPickerSummaryResolve(IReadOnlyList<string> labels, string empty) =>
        QPicker.QPickerSummaryResolve(labels, empty);

    internal static QLog TLogCreate() => new(TAtelierCreate().CAtelierUsher);
    internal static LTraceEntry TTraceEntryCreate(LTraceKind kind, string summary, string? detail, double? span) =>
        new("12:00:00.000", string.Empty, kind, summary, detail, span);
    internal static QLogRow TLogRowCreate(LTraceEntry entry) => new(entry);
    internal static void TLogFileSet(QLog log, string path, string livePath) => log.QLogFileSet(path, livePath);
    internal static bool TLogLiveSet(QLog log, string livePath) => log.QLogLiveSet(livePath);
    internal static bool TLogFileCheck(QLog log, string path) => log.QLogFileCheck(path);
    internal static void TLogFollowSet(QLog log, bool follow) => log.QLogFollowSet(follow);
    internal static void TLogScrollHandle(
        QLog log, bool feed, double extent, double? scrollable, double? offset, double row) =>
        log.QLogScrollHandle(feed, extent, scrollable, offset, row);
    internal static void TLogSnapshotSet(QLog log, long sequence) => log.QLogSnapshotSet(sequence);
    internal static bool TLogSnapshotCheck(QLog log, long sequence) => log.QLogSnapshotCheck(sequence);
    internal static bool TLogExpandToggle(QLog log, QLogRow? row) => log.QLogExpandToggle(row);
    internal static void TLogCategorySet(QLog log, IReadOnlyList<string> tokens) => log.QLogCategorySet(tokens);
    internal static void TLogResetAttach(QLog log, Action handler) => log.QLogRowsRebuilt += handler;
    internal static string TLogKeyRead(LTraceKind kind) => QLogRow.QLogKeyRead(kind);

    // The atelier as the host builds it.
    internal static CAtelier TAtelierCreate() =>
        new(new LAtelierPort
        {
            LAtelierUsher = new LUsherOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierLineage = new LLineageOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierWorkspace = new LWorkspaceOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierPlacement = new LPlacementOutlet(LPreferenceStateStore.LPreferencePlacementRead),
            LAtelierViewer = new LViewerOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierEdit = new LEditOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierRoster = new LRosterOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierFix = new LFixOutlet(),
            LAtelierConvert = new LConvertOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierTone = new LToneOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierEqualizer = new LEqualizerOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierClinic = new LClinicOutlet(),
            LAtelierRosterCard = new LRosterCardOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierMerge = new LMergeOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierNoise = new LNoiseOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierConsole = new LConsoleOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierFlow = new LFlowOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierMap = new LMapOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierRosterDetail = new LRosterDetailOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierWhitebalance = new LWhitebalanceOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierFilter = new LFilterOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierAudio = new LAudioOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierLoudness = new LLoudnessOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierVideo = new LVideoOutlet(Interface.TInterface.TInterfaceEngine),
            LAtelierScene = TSceneOutletCreate(),
            LAtelierList = TInterfaceList.TListOutletCreate,
            LAtelierRelay = TInterfaceList.TRelayOutletCreate(),
        });
}
