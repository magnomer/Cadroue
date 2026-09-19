using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LRail TRailCreate(LStrip strip) => new(strip);
    internal static bool TRailPressHandle(
        LRail rail, LStripTab? tab, int clicks, bool nameHit, double x, double y, double offsetX, double offsetY) =>
        rail.LRailPressHandle(tab, clicks, nameHit, x, y, offsetX, offsetY);
    internal static bool TRailMoveCheck(LRail rail, bool pressed) => rail.LRailMoveCheck(pressed);
    internal static bool TRailDragResolve(LRail rail, double x, double y, double minimumX, double minimumY) =>
        rail.LRailDragResolve(x, y, minimumX, minimumY);
    internal static void TRailDragMove(LRail rail, double pointer, IReadOnlyList<double> centers) =>
        rail.LRailDragMove(pointer, centers);
    internal static LStripTab? TRailReleaseResolve(LRail rail) => rail.LRailReleaseResolve();
    internal static int TRailIndexResolve(double pointer, IReadOnlyList<double> centers) =>
        LRail.LRailIndexResolve(pointer, centers);
    internal static bool TRailInsideCheck(bool minimized, double x, double y, double width, double height) =>
        LRail.LRailInsideCheck(minimized, x, y, width, height);
    internal static bool TRailNameCheck(bool visible, double x, double y, double width, double height) =>
        LRail.LRailNameCheck(visible, x, y, width, height);
    internal static bool TRailKeyHandle(LRail rail, LStripTab? tab, bool enter, bool escape, string text) =>
        rail.LRailKeyHandle(tab, enter, escape, text);
    internal static void TRailOutsideHandle(LRail rail, LStripTab? tab, bool inside, string? text) =>
        rail.LRailOutsideHandle(tab, inside, text);

    internal static LChrome TChromeCreate() => new();
    internal static void TChromePressHandle(LChrome chrome, bool caption, double x, double y) =>
        chrome.LChromePressHandle(caption, x, y);
    internal static bool TChromeMoveResolve(
        LChrome chrome, double x, double y, bool pressed, double minimumX, double minimumY) =>
        chrome.LChromeMoveResolve(x, y, pressed, minimumX, minimumY);
    internal static bool TChromeReleaseResolve(LChrome chrome, bool logo) => chrome.LChromeReleaseResolve(logo);
    internal static void TChromeReset(LChrome chrome) => chrome.LChromeReset();
    internal static bool TChromeCaptionResolve(bool interactive, bool tab, bool inside) =>
        LChrome.LChromeCaptionResolve(interactive, tab, inside);
    internal static bool TChromeDoubleCheck(int clicks, bool caption) => LChrome.LChromeDoubleCheck(clicks, caption);

    internal static LWorkspace TWorkspaceCreate(string key, LPreset? preset) => new(key, preset);
    internal static void TWorkspaceAttach(
        LWorkspace workspace, LDocket? docket, LSegment? segment, LFlow? flow, LViewer? viewer) =>
        workspace.LWorkspaceAttach(docket, segment, flow, viewer, null, () => new LSceneTabRecord());
    internal static void TWorkspaceClose(LWorkspace workspace) => workspace.LWorkspaceClose();
    internal static bool TWorkspaceBusyCheck(LWorkspace workspace) => workspace.LWorkspaceBusyCheck();
    internal static IReadOnlyList<string> TDocketPathsRead(LDocket docket) => docket.LDocketPathsRead();
    internal static LHistoryEntry TWorkspaceSnapshotRead(LWorkspace workspace) => workspace.LWorkspaceSnapshotRead();
    internal static bool TWorkspaceUndo(LWorkspace workspace) => workspace.LWorkspaceUndo();
    internal static bool TWorkspaceRedo(LWorkspace workspace) => workspace.LWorkspaceRedo();
    internal static void TWorkspacePresetSelect(LWorkspace workspace, LPresetRecord record) =>
        workspace.LWorkspacePresetOwner.LPresetSelectionValue = record;
    internal static LPresetRecord TPresetRecordCreate(string display) => new() { LPresetDisplay = display };
    internal static LRelay TWorkspaceRelayCreate(LWorkspace workspace, string custom, double left, double top) =>
        workspace.LWorkspaceRelayCreate(custom, left, top);
    internal static void TWorkspaceRelayApply(LWorkspace workspace, LRelay relay) =>
        workspace.LWorkspaceRelayApply(relay);
    internal static void TWorkspaceSourceRun(LWorkspace workspace) => workspace.LWorkspaceSourceRun();
    internal static void TWorkspaceRelayRestore(LWorkspace workspace, LRelay relay, TimeSpan duration) =>
        workspace.LWorkspaceRelayRestore(relay, duration);
    internal static bool TWorkspaceMediaClear(LWorkspace workspace, IReadOnlySet<Guid> cohorts) =>
        workspace.LWorkspaceMediaClear(cohorts);
    internal static void TWorkspacePathHandle(LWorkspace workspace, string? path) =>
        workspace.LWorkspacePathHandle(path);
    internal static void TWorkspacePathsAttach(LWorkspace workspace, Action<IReadOnlyList<string>> handler) =>
        workspace.LWorkspacePathsAdd += handler;
    internal static void TWorkspaceSelectAttach(LWorkspace workspace, Action<string> handler) =>
        workspace.LWorkspaceSourceSelect += handler;
    internal static void TWorkspaceOpenAttach(LWorkspace workspace, Action<string> handler) =>
        workspace.LWorkspaceSourceOpen += handler;
    internal static void TWorkspaceCloseAttach(LWorkspace workspace, Action handler) =>
        workspace.LWorkspaceMediaClose += handler;
    internal static void TWorkspaceVolumeAttach(LWorkspace workspace, Action<double> handler) =>
        workspace.LWorkspaceVolumeApply += handler;
    internal static void TWorkspaceSeekAttach(LWorkspace workspace, Action<TimeSpan> handler) =>
        workspace.LWorkspaceSeekApply += handler;
    internal static void TWorkspaceRangeAttach(LWorkspace workspace, Action<TimeSpan, TimeSpan> handler) =>
        workspace.LWorkspaceRangeApply += handler;

    internal static LSurface TSurfaceCreate(LColumn column, int count, int exportIndex) =>
        new(column, count, exportIndex);
    internal static LSceneTabRecord TSurfaceLayoutRead(LSurface surface) => surface.LSurfaceLayoutRead();
    internal static IReadOnlyList<int> TSurfaceCollapsedResolve(LSurface surface, LSceneTabRecord? layout) =>
        surface.LSurfaceCollapsedResolve(layout);
    internal static bool TSurfaceToggleResolve(LSurface surface) => surface.LSurfaceToggleResolve();
    internal static bool TSurfaceCollapsedCheck(LSurface surface, int index) => surface.LSurfaceCollapsedCheck(index);
    internal static double TSurfaceWidthResolve(LSurface surface, double total) => surface.LSurfaceWidthResolve(total);
    internal static double TSurfaceMinimumResolve(double element, double table) =>
        LSurface.LSurfaceMinimumResolve(element, table);
    internal static IReadOnlyList<int> TSurfaceSplittersResolve(int count) => LSurface.LSurfaceSplittersResolve(count);
    internal static int TSurfaceGutterResolve(int index) => LSurface.LSurfaceGutterResolve(index);
    internal static LSceneTabRecord TSceneTabCreate(bool exportHidden, bool autoRelay, params int[] collapsed) => new()
    {
        LSceneExportHidden = exportHidden,
        LSceneAutoRelay = autoRelay,
        LScenePanelsCollapsed = collapsed.ToList()
    };

    internal static LPiece TPieceCreate(TimeSpan start, TimeSpan end) => new(start, end, 0, string.Empty);

    internal static LDeck TDeckCreate(LStrip strip) => new(strip);
    internal static void TDeckAttach(LDeck deck, Action<LStripTab> hide, Action<LStripTab> show, Action empty)
    {
        deck.LDeckHide += hide;
        deck.LDeckShow += show;
        deck.LDeckEmptyChange += empty;
    }
    internal static void TDeckClose(LDeck deck) => deck.LDeckClose();

    internal static LStripTab TStripKeyCreate(string key) => LStrip.LStripTabCreate(key);
    internal static string TStripKeyResolve(string key) => LStrip.LStripKeyResolve(key);
    internal static bool TStripCloseConfirm(LStrip strip, LStripTab? tab) => strip.LStripCloseConfirm(tab);
    internal static void TStripClose(LStrip strip, LStripTab tab) => strip.LStripClose(tab);
    internal static void TStripAllClose(LStrip strip) => strip.LStripAllClose();
    internal static void TStripCloseAttach(LStrip strip, Action<LStripTab> handler) => strip.LStripTabClose += handler;
    internal static void TStripAddAttach(LStrip strip, Action<LStripTab> handler) => strip.LStripTabAdd += handler;
    internal static IReadOnlyList<LStripTab> TStripRelayRead(LStrip strip, Guid source) =>
        strip.LStripRelayRead(source);
    internal static void TStripPendingSet(LStrip strip, LStripTab tab, bool pending) =>
        strip.LStripPendingSet(tab, pending);
    internal static void TStripEditSet(LStrip strip, LStripTab tab, bool editing) =>
        strip.LStripEditSet(tab, editing);
}
