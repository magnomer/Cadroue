using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceList
{
    internal static LDocket TDocketCreate() => new();
    internal static int TDocketPathsAdd(LDocket docket, params string[] paths) => docket.LDocketPathsAdd(paths);
    internal static int TDocketPathsRemove(LDocket docket, params string[] paths) => docket.LDocketPathsRemove(paths);
    internal static QList TListCreate() => new(TInterface.TAtelierCreate().CAtelierUsher);
    internal static void TListPathAttach(QList list, Action<string?> handler) => list.QListPathChanged += handler;
    internal static void TListSelect(QList list, string? path) => list.QListSelect(path);
    internal static void TListPressSelect(QList list, string path, bool shift, bool control) =>
        list.QListPressSelect(path, shift, control);
    internal static void TListReleaseSelect(QList list) => list.QListReleaseSelect();
    internal static void TListAllSelect(QList list) => list.QListAllSelect();
    internal static IReadOnlyList<string> TListSelectionRead(QList list) => list.QListSelectionRead();
    internal static void TListSuccessorSet(QList list, params string[] removed) =>
        list.QListSuccessor.QListSuccessorSet(
            removed, list.QListDocket.LDocketPathsRead(), list.QListIndexRead(removed[0]));
    internal static void TListSuccessorReset(QList list) => list.QListSuccessor.QListSuccessorReset();
    internal static void TListRemovedApply(QList list, params string[] removed) => list.QListRemovedApply(removed);
    internal static void TListChangeAttach(QList list, Action handler) => list.QListChanged += handler;
    internal static void TListClearAttach(QList list, Action<IReadOnlyList<string>> handler) =>
        list.QListClearChanged += handler;
    internal static void TListItemsAttach(QList list, Action<IReadOnlyList<LDocketEntry>> handler) =>
        list.QListItemsAdded += handler;
    internal static void TListLockAttach(QList list, Action<bool> handler) => list.QListLockChanged += handler;
    internal static Task<int> TListPathsAdd(QList list, params string[] paths) => list.QListPathsAdd(paths);
    internal static void TListDialogAdd(QList list, bool? confirmed, IReadOnlyList<string> paths) =>
        list.QListDialogAdd(confirmed, paths, "file");
    internal static void TListRemove(QList list) => list.QListRemove();
    internal static void TListClear(QList list) => list.QListClear();
    internal static bool TListLockCheck(QList list) => list.QListLockCheck();
    internal static IReadOnlyList<QListCard> TListCardsRead(QList list) => list.QListFace.QListCardsRead();
    internal static string TListStateRead(QList list, string path) => list.QListFace.QListStateRead(path);
    internal static bool TListPressHandle(
        QList list, string path, bool shift, bool control, double x, double y, double grabX, double grabY) =>
        list.QListDrag.QListPressHandle(path, shift, control, x, y, grabX, grabY);
    internal static bool TListDragResolve(QList list, double x, double y, double minimum, bool pressed) =>
        list.QListDrag.QListDragResolve(x, y, minimum, minimum, pressed);
    internal static void TListReleaseHandle(QList list) => list.QListDrag.QListReleaseHandle();
    internal static bool TListKeyRun(QList list, string key, bool control) => list.QListKeyRun(key, control);

    internal static int TDocketDeliveredAdd(LDocket docket, string path, bool locked) =>
        docket.LDocketDeliveredAdd([path], Guid.NewGuid(), locked);

    internal static IReadOnlyList<LDocketEntry> TDocketItemsRead(LDocket docket) => docket.LDocketItemsRead();

    internal static void TListRelayAttach(QStrip strip) =>
        QListRelay.QListRelayAttach(strip, TInterface.TAtelierCreate().CAtelierUsher);
    internal static bool TListDeliveredAdd(Guid tab, string path, Guid batch) =>
        QListRelay.QListDeliveredAdd(tab, path, batch);
    internal static bool TListDeliveredCommit(Guid tab, string path, Guid batch) =>
        QListRelay.QListDeliveredCommit(tab, path, batch);
    internal static void TListDeliveredRemove(LWorkItem work, bool force) =>
        QListRelay.QListDeliveredRemove(work, force);
    internal static void TListBatchRemove(params Guid[] batches) => QListRelay.QListBatchRemove(batches);
    internal static bool TListSourceClaim(params LWorkItem[] accepted) => QListRelay.QListSourceClaim(accepted);
    internal static void TListSourceRelease(
        IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)> unlocks) =>
        QListRelay.QListSourceRelease(unlocks);

    internal static int TDocketDeliveredAdd(LDocket docket, string path, Guid batch, bool locked) =>
        docket.LDocketDeliveredAdd([path], batch, locked);
    internal static LWorkItem TWorkRelayCreate(string source, Guid batch, Guid relaySource) =>
        new(
            batch, LWorkKind.LWorkKindConvert, LWorkPriority.LWorkPriorityNormal, source,
            TimeSpan.Zero, TimeSpan.Zero, "x.mp4", @"C:\out\x.mp4", TEncodeCommand.TOutputCreate())
        {
            LWorkRelaySource = relaySource
        };
}
