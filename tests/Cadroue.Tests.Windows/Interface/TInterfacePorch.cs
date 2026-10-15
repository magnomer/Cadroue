using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static QRail TRailRead(QStrip strip) => strip.QStripRail;
    internal static bool TRailPressHandle(
        QRail rail, QStripTab? tab, int clicks, bool nameHit, double x, double y, double offsetX, double offsetY) =>
        rail.QRailPressHandle(tab, clicks, nameHit, x, y, offsetX, offsetY);
    internal static bool TRailMoveCheck(QRail rail, bool pressed) => rail.QRailMoveCheck(pressed);
    internal static bool TRailDragResolve(QRail rail, double x, double y, double minimumX, double minimumY) =>
        rail.QRailDragResolve(x, y, minimumX, minimumY);
    internal static void TRailDragMove(QRail rail, double pointer, IReadOnlyList<double> centers) =>
        rail.QRailDragMove(pointer, centers);
    internal static QStripTab? TRailReleaseResolve(QRail rail) => rail.QRailReleaseResolve();
    internal static int TRailIndexResolve(double pointer, IReadOnlyList<double> centers) =>
        QRail.QRailIndexResolve(pointer, centers);
    internal static bool TRailInsideCheck(bool minimized, double x, double y, double width, double height) =>
        QRail.QRailInsideCheck(minimized, x, y, width, height);
    internal static bool TRailNameCheck(bool visible, double x, double y, double width, double height) =>
        QRail.QRailNameCheck(visible, x, y, width, height);
    internal static bool TRailKeyHandle(QRail rail, QStripTab? tab, bool enter, bool escape, string text) =>
        rail.QRailKeyHandle(tab, enter, escape, text);
    internal static void TRailOutsideHandle(QRail rail, QStripTab? tab, bool inside, string? text) =>
        rail.QRailOutsideHandle(tab, inside, text);

    internal static QChrome TChromeCreate() => new();
    internal static void TChromePressHandle(QChrome chrome, bool caption, double x, double y) =>
        chrome.QChromePressHandle(caption, x, y);
    internal static bool TChromeMoveResolve(
        QChrome chrome, double x, double y, bool pressed, double minimumX, double minimumY) =>
        chrome.QChromeMoveResolve(x, y, pressed, minimumX, minimumY);
    internal static bool TChromeReleaseResolve(QChrome chrome, bool logo) => chrome.QChromeReleaseResolve(logo);
    internal static void TChromeReset(QChrome chrome) => chrome.QChromeReset();
    internal static bool TChromeCaptionResolve(bool interactive, bool tab, bool inside) =>
        QChrome.QChromeCaptionResolve(interactive, tab, inside);
    internal static bool TChromeDoubleCheck(int clicks, bool caption) => QChrome.QChromeDoubleCheck(clicks, caption);

    internal static QWorkspace TWorkspaceCreate(string key, LPreset? preset)
    {
        CAtelier atelier = TAtelierCreate();
        QWorkspace workspace = new(
            key, atelier.CAtelierUsher, atelier.CAtelierInfo, atelier.CAtelierLineage,
            atelier.CAtelierWorkspaceCreate());
        if (preset is not null)
        {
            workspace.QWorkspacePresetAccept(preset);
        }

        return workspace;
    }
    internal static void TWorkspaceAttach(QWorkspace workspace) =>
        workspace.QWorkspaceAttach(() => new LSceneTabEntry());
    internal static LSegment TWorkspaceSegmentRead(QWorkspace workspace) =>
        workspace.QWorkspaceFlow!.QFlowSection.QFlowSegment;
    internal static void TWorkspaceClose(QWorkspace workspace) => workspace.QWorkspaceClose();
    internal static bool TWorkspaceBusyCheck(QWorkspace workspace) => workspace.QWorkspaceBusyCheck();
    internal static IReadOnlyList<string> TDocketPathsRead(LDocket docket) => docket.LDocketPathsRead();
    internal static LHistoryEntry TWorkspaceSnapshotRead(QWorkspace workspace) => workspace.QWorkspaceSnapshotRead();
    internal static bool TWorkspaceUndo(QWorkspace workspace) => workspace.QWorkspaceUndo();
    internal static bool TWorkspaceRedo(QWorkspace workspace) => workspace.QWorkspaceRedo();
    internal static void TWorkspacePresetSelect(QWorkspace workspace, LPresetDocument record) =>
        workspace.QWorkspacePresetOwner.LPresetSelectionValue = record;
    internal static LPresetDocument TPresetRecordCreate(string display) => new() { LPresetDisplay = display };
    internal static LRelay TWorkspaceRelayCreate(QWorkspace workspace, string custom, double left, double top) =>
        workspace.QWorkspaceRelay.QWorkspaceRelayCreate(custom, left, top);
    internal static void TWorkspaceRelayApply(QWorkspace workspace, LRelay relay) =>
        workspace.QWorkspaceRelay.QWorkspaceRelayApply(relay);
    internal static void TWorkspaceSourceRun(QWorkspace workspace) => workspace.QWorkspaceRelay.QWorkspaceSourceRun();
    internal static void TWorkspaceRelayRestore(QWorkspace workspace, LRelay relay, TimeSpan duration) =>
        workspace.QWorkspaceRelay.QWorkspaceRelayRestore(relay, duration);
    internal static bool TWorkspaceMediaClear(QWorkspace workspace, IReadOnlySet<Guid> cohorts) =>
        workspace.QWorkspaceMediaClear(cohorts);
    internal static void TWorkspacePathHandle(QWorkspace workspace, string? path) =>
        workspace.QWorkspacePathHandle(path);
    internal static void TWorkspacePathsAttach(QWorkspace workspace, Action<IReadOnlyList<string>> handler) =>
        workspace.QWorkspaceRelay.QWorkspacePathsAdded += handler;
    internal static void TWorkspaceSelectAttach(QWorkspace workspace, Action<string> handler) =>
        workspace.QWorkspaceRelay.QWorkspaceSourceSelected += handler;
    internal static void TWorkspaceOpenAttach(QWorkspace workspace, Action<string> handler) =>
        workspace.QWorkspaceRelay.QWorkspaceSourceOpened += handler;
    internal static void TWorkspaceCloseAttach(QWorkspace workspace, Action handler) =>
        workspace.QWorkspaceMediaClosed += handler;
    internal static void TWorkspaceVolumeAttach(QWorkspace workspace, Action<double> handler) =>
        workspace.QWorkspaceRelay.QWorkspaceVolumeApplied += handler;
    internal static void TWorkspaceSeekAttach(QWorkspace workspace, Action<TimeSpan> handler) =>
        workspace.QWorkspaceRelay.QWorkspaceSeekApplied += handler;
    internal static void TWorkspaceRangeAttach(QWorkspace workspace, Action<TimeSpan, TimeSpan> handler) =>
        workspace.QWorkspaceRelay.QWorkspaceRangeApplied += handler;

    internal static QSurface TSurfaceCreate(IReadOnlyList<double> minimums, int exportIndex) =>
        new(null, minimums, null, null, -1, exportIndex);
    internal static QColumnPlan TSurfacePlanRead(QSurface surface) => surface.QSurfacePlan;
    internal static LSceneTabEntry TSurfaceLayoutRead(QSurface surface) => surface.QSurfaceLayoutRead();
    internal static IReadOnlyList<int> TSurfaceCollapsedResolve(QSurface surface, LSceneTabEntry? layout) =>
        surface.QSurfaceCollapsedResolve(layout);
    internal static bool TSurfaceToggleResolve(QSurface surface) => surface.QSurfaceToggleResolve();
    internal static bool TSurfaceCollapsedCheck(QSurface surface, int index) => surface.QSurfaceCollapsedCheck(index);
    internal static double TSurfaceWidthResolve(QSurface surface, double total) => surface.QSurfaceWidthResolve(total);
    internal static double TSurfaceMinimumResolve(double element, double table) =>
        QSurface.QSurfaceMinimumResolve(element, table);
    internal static IReadOnlyList<int> TSurfaceSplittersResolve(int count) => QSurface.QSurfaceSplittersResolve(count);
    internal static int TSurfaceGutterResolve(int index) => QSurface.QSurfaceGutterResolve(index);
    internal static LSceneTabEntry TSceneTabCreate(bool exportHidden, bool autoRelay, params int[] collapsed) => new()
    {
        LSceneExportHidden = exportHidden,
        LSceneAutoRelay = autoRelay,
        LScenePanelsCollapsed = collapsed.ToList()
    };

    internal static LPiece TPieceCreate(TimeSpan start, TimeSpan end) => new(start, end, 0, string.Empty);

    internal static QDeck TDeckRead(QStrip strip) => strip.QStripDeck;
    internal static void TDeckAttach(QDeck deck, Action<QStripTab> hide, Action<QStripTab> show, Action empty)
    {
        deck.QDeckHidden += hide;
        deck.QDeckRevealed += show;
        deck.QDeckEmptyChanged += empty;
    }
    internal static void TDeckClose(QDeck deck) => deck.QDeckClose();

    internal static QStripTab TStripKeyCreate(string key) =>
        new QStrip(
            TAtelierCreate().CAtelierUsher, TAtelierCreate().CAtelierInfo, TAtelierCreate().CAtelierLineage,
            TAtelierCreate().CAtelierWorkspaceCreate).QStripTabCreate(key);
    internal static string TStripKeyResolve(string key) => QStrip.QStripKeyResolve(key);
    internal static bool TStripCloseConfirm(QStrip strip, QStripTab? tab) =>
        strip.QStripClosure.QStripCloseConfirm(tab);
    internal static void TStripClose(QStrip strip, QStripTab tab) => strip.QStripClosure.QStripClose(tab);
    internal static void TStripAllClose(QStrip strip) => strip.QStripClosure.QStripAllClose();
    internal static void TStripCloseAttach(QStrip strip, Action<QStripTab> handler) =>
        strip.QStripClosure.QStripTabClosed += handler;
    internal static void TStripAddAttach(QStrip strip, Action<QStripTab> handler) => strip.QStripTabAdded += handler;
    internal static IReadOnlyList<QStripTab> TStripRelayRead(QStrip strip, Guid source) =>
        strip.QStripRelay.QStripRelayRead(source);
    internal static void TStripPendingSet(QStrip strip, QStripTab tab, bool pending) =>
        strip.QStripPendingSet(tab, pending);
    internal static void TStripEditSet(QStrip strip, QStripTab tab, bool editing) =>
        strip.QStripEditSet(tab, editing);
}
