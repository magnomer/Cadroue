using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LWindow TWindowCreate(LStrip strip, Func<Func<bool>, bool> dispatch) => new(strip, dispatch);
    internal static void TWindowTabSet(LWindow window, LStripTab? tab, LViewer? viewer) =>
        window.LWindowTabSet(tab, viewer);
    internal static void TWindowAddAttach(LWindow window, Action<LWindowTab> handler) =>
        window.LWindowTabAdd += handler;
    internal static void TWindowSelectAttach(LWindow window, Action<LStripTab, double> attach, Action<LStripTab> detach)
    {
        window.LWindowTabAttach += attach;
        window.LWindowTabDetach += detach;
    }
    internal static LSceneRecord TWindowSceneRead(LWindow window, string name) => window.LWindowSceneRead(name);
    internal static (double, double) TWindowWidthResolve(
        LWindow window, double? required, double reserved, double width) =>
        window.LWindowWidthResolve(required, reserved, width);
    internal static void TStripWorkspaceAttach(LStripTab tab, LPreset preset, LDocket? docket) =>
        tab.LStripWorkspaceAttach(preset, docket, () => new LSceneTabRecord());
    internal static LPreset TPresetInitialCreate(string key) => LPreset.LPresetInitialCreate(key);

    internal static LWindowDrag TWindowDragCreate(
        string[]? paths, bool group, string? source, bool copy, object? data) =>
        new(paths, group, source, copy, data);
    internal static void TWindowOverHandle(LWindow window, LWindowDrag drag) =>
        window.LWindowDrop.LWindowOverHandle(drag);
    internal static void TWindowDropHandle(LWindow window, LWindowDrag drag) =>
        window.LWindowDrop.LWindowDropHandle(drag);
    internal static LWindowDropEffect TWindowEffectRead(LWindow window) => window.LWindowDrop.LWindowDropEffect;
    internal static bool TWindowHandledRead(LWindow window) => window.LWindowDrop.LWindowDropHandled;
    internal static void TWindowDropAttach(LWindow window, Action<IReadOnlyList<string>> list, Action<string> viewer)
    {
        window.LWindowDrop.LWindowListAdd += list;
        window.LWindowDrop.LWindowViewerOpen += viewer;
    }

    internal static void TWindowShortcutAttach(
        LWindow window,
        Action show,
        Func<bool?> undo,
        Func<bool?> redo,
        Func<bool> unload,
        Func<IReadOnlySet<Guid>, bool?> clear,
        Func<string, bool?> flow) =>
        window.LWindowShortcut.LWindowShortcutAttach(show, undo, redo, unload, clear, flow);
    internal static bool TWindowShortcutRun(LWindow window, string token) =>
        window.LWindowShortcut.LWindowShortcutRun(token);
    internal static void TWindowPlayAttach(LWindow window, Action play, Action pause)
    {
        window.LWindowShortcut.LWindowShortcutPlay += play;
        window.LWindowShortcut.LWindowShortcutPause += pause;
    }

    internal static LWindowPress TWindowPressCreate(
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
    internal static bool TWindowShortcutHandle(LWindow window, LWindowPress press) =>
        window.LWindowShortcut.LWindowShortcutHandle(press);

    internal static LSash TSashCreate(double border) => new(border);
    internal static LSashBounds TSashBoundsCreate(double left, double top, double width, double height) =>
        new(left, top, width, height);
    internal static int TSashDirectionResolve(double x, double y, double width, double height, double border) =>
        LSash.LSashDirectionResolve(x, y, width, height, border);
    internal static bool TSashPressHandle(
        LSash sash,
        bool normal,
        bool interactive,
        double x,
        double y,
        double pointerX,
        double pointerY,
        LSashBounds bounds) =>
        sash.LSashPressHandle(normal, interactive, x, y, pointerX, pointerY, bounds);
    internal static bool TSashMoveHandle(
        LSash sash,
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
        sash.LSashMoveHandle(normal, interactive, x, y, width, height, pointerX, pointerY, minimumWidth, minimumHeight);
    internal static bool TSashReleaseHandle(LSash sash) => sash.LSashReleaseHandle();
    internal static void TSashCaptureHandle(LSash sash) => sash.LSashCaptureHandle();
    internal static int TSashLeaveResolve(LSash sash) => sash.LSashLeaveResolve();
    internal static void TSashBoundsAttach(LSash sash, Action<LSashBounds> handler) =>
        sash.LSashBoundsChange += handler;
    internal static void TSashCaptureAttach(LSash sash, Action start, Action stop, Action<bool> active)
    {
        sash.LSashCaptureStart += start;
        sash.LSashCaptureStop += stop;
        sash.LSashActiveChange += active;
    }

    internal static LSashBounds TSashBoundsClamp(
        LSashBounds bounds,
        LSashBounds work,
        double minWidth,
        double minHeight,
        double maxWidth,
        double maxHeight) =>
        LSash.LSashBoundsClamp(bounds, work, minWidth, minHeight, maxWidth, maxHeight);
    internal static LSashBounds TSashPlacementResolve(
        double? left,
        double? top,
        double width,
        double height,
        LSashBounds current,
        double minWidth,
        double minHeight,
        double workLeft,
        double workTop) =>
        LSash.LSashPlacementResolve(left, top, width, height, current, minWidth, minHeight, workLeft, workTop);
    internal static (double, double) TSashRelayResolve(
        double dropLeft,
        double dropTop,
        double screenLeft,
        double screenTop,
        double screenWidth,
        double screenHeight) =>
        LSash.LSashRelayResolve(dropLeft, dropTop, screenLeft, screenTop, screenWidth, screenHeight);
    internal static (double, double) TSashCenterResolve(bool manual, LSashBounds window, LSashBounds owner) =>
        LSash.LSashCenterResolve(manual, window, owner);

    internal static IReadOnlyList<LTokenPart> TTokenParse(string text) => LToken.LTokenParse(text);
    internal static LTokenPart TTokenPartResolve(string token) => LToken.LTokenPartResolve(token);
    internal static string TTokenTextRead(IEnumerable<(string?, string?)> inlines) => LToken.LTokenTextRead(inlines);
    internal static LToken TTokenCreate() => new();
    internal static bool TTokenDropHandle(LToken token, string? text) => token.LTokenDropHandle(text);
    internal static void TTokenInsertAttach(LToken token, Action<string> handler) => token.LTokenInsert += handler;

    internal static LGhost TGhostCreate(double grabX, double grabY) => new(grabX, grabY);
    internal static void TGhostPointSet(LGhost ghost, double x, double y) => ghost.LGhostPointSet(x, y);
    internal static bool TGhostClear(LGhost ghost) => ghost.LGhostClear();
    internal static LGhostSize TGhostSizeResolve(double width, double height, double scaleX, double scaleY) =>
        LGhost.LGhostSizeResolve(width, height, scaleX, scaleY);

    internal static IReadOnlyList<LPickerItem> TPickerItemsCreate(
        IReadOnlyList<string> tokens, IReadOnlyList<string> selected) =>
        LPicker.LPickerItemsCreate(tokens, selected);
    internal static (string, bool) TPickerSummaryResolve(IReadOnlyList<string> labels, string empty) =>
        LPicker.LPickerSummaryResolve(labels, empty);

    internal static LLog TLogCreate() => new();
    internal static LTraceEntry TTraceEntryCreate(LTraceKind kind, string summary, string? detail, double? span) =>
        new("12:00:00.000", string.Empty, kind, summary, detail, span);
    internal static LLogRow TLogRowCreate(LTraceEntry entry) => new(entry);
    internal static void TLogFileSet(LLog log, string path, string livePath) => log.LLogFileSet(path, livePath);
    internal static bool TLogLiveSet(LLog log, string livePath) => log.LLogLiveSet(livePath);
    internal static bool TLogFileCheck(LLog log, string path) => log.LLogFileCheck(path);
    internal static void TLogFollowSet(LLog log, bool follow) => log.LLogFollowSet(follow);
    internal static void TLogScrollHandle(
        LLog log, bool feed, double extent, double? scrollable, double? offset, double row) =>
        log.LLogScrollHandle(feed, extent, scrollable, offset, row);
    internal static void TLogSnapshotSet(LLog log, long sequence) => log.LLogSnapshotSet(sequence);
    internal static bool TLogSnapshotCheck(LLog log, long sequence) => log.LLogSnapshotCheck(sequence);
    internal static bool TLogExpandToggle(LLog log, LLogRow? row) => log.LLogExpandToggle(row);
    internal static void TLogCategorySet(LLog log, IReadOnlyList<string> tokens) => log.LLogCategorySet(tokens);
    internal static void TLogResetAttach(LLog log, Action handler) => log.LLogRowsReset += handler;
    internal static string TLogKeyRead(LTraceKind kind) => LLogRow.LLogKeyRead(kind);
}
