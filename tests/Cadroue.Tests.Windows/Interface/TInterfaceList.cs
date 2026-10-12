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
    internal static LList TListCreate() => new(TInterface.TAtelierCreate().CAtelierUsher);
    internal static void TListPathAttach(LList list, Action<string?> handler) => list.LListPathChanged += handler;
    internal static void TListSelect(LList list, string? path) => list.LListSelect(path);
    internal static void TListPressSelect(LList list, string path, bool shift, bool control) =>
        list.LListPressSelect(path, shift, control);
    internal static void TListReleaseSelect(LList list) => list.LListReleaseSelect();
    internal static void TListAllSelect(LList list) => list.LListAllSelect();
    internal static IReadOnlyList<string> TListSelectionRead(LList list) => list.LListSelectionRead();
    internal static void TListSuccessorSet(LList list, params string[] removed) => list.LListSuccessorSet(removed);
    internal static void TListSuccessorReset(LList list) => list.LListSuccessorReset();
    internal static void TListRemovedApply(LList list, params string[] removed) => list.LListRemovedApply(removed);
    internal static void TListSuccessorSelect(LList list) => list.LListSuccessorSelect();
    internal static void TListChangeAttach(LList list, Action handler) => list.LListChanged += handler;
    internal static void TListClearAttach(LList list, Action<IReadOnlyList<string>> handler) =>
        list.LListClearChanged += handler;
    internal static void TListItemsAttach(LList list, Action<IReadOnlyList<LDocketEntry>> handler) =>
        list.LListItemsAdded += handler;
    internal static void TListLockAttach(LList list, Action<bool> handler) => list.LListLockChanged += handler;
    internal static Task<int> TListPathsAdd(LList list, params string[] paths) => list.LListPathsAdd(paths);
    internal static void TListDialogAdd(LList list, bool? confirmed, IReadOnlyList<string> paths) =>
        list.LListDialogAdd(confirmed, paths, "file");
    internal static void TListRemove(LList list) => list.LListRemove();
    internal static void TListClear(LList list) => list.LListClear();
    internal static bool TListLockCheck(LList list) => list.LListLockCheck();
    internal static IReadOnlyList<LListCard> TListCardsRead(LList list) => list.LListFace.LListCardsRead();
    internal static string TListStateRead(LList list, string path) => list.LListFace.LListStateRead(path);
    internal static bool TListPressHandle(
        LList list, string path, bool shift, bool control, double x, double y, double grabX, double grabY) =>
        list.LListDrag.LListPressHandle(path, shift, control, x, y, grabX, grabY);
    internal static bool TListDragResolve(LList list, double x, double y, double minimum, bool pressed) =>
        list.LListDrag.LListDragResolve(x, y, minimum, minimum, pressed);
    internal static void TListReleaseHandle(LList list) => list.LListDrag.LListReleaseHandle();
    internal static bool TListKeyRun(LList list, string key, bool control) => list.LListKeyRun(key, control);

    internal static int TDocketDeliveredAdd(LDocket docket, string path, bool locked) =>
        docket.LDocketDeliveredAdd([path], Guid.NewGuid(), locked);

    internal static IReadOnlyList<LDocketEntry> TDocketItemsRead(LDocket docket) => docket.LDocketItemsRead();

    internal static void TListRelayAttach(LStrip strip) =>
        LListRelay.LListRelayAttach(strip, TInterface.TAtelierCreate().CAtelierUsher);
    internal static bool TListDeliveredAdd(Guid tab, string path, Guid batch) =>
        LListRelay.LListDeliveredAdd(tab, path, batch);
    internal static bool TListDeliveredCommit(Guid tab, string path, Guid batch) =>
        LListRelay.LListDeliveredCommit(tab, path, batch);
    internal static void TListDeliveredRemove(LWorkItem work, bool force) =>
        LListRelay.LListDeliveredRemove(work, force);
    internal static void TListBatchRemove(params Guid[] batches) => LListRelay.LListBatchRemove(batches);
    internal static bool TListSourceClaim(params LWorkItem[] accepted) => LListRelay.LListSourceClaim(accepted);
    internal static void TListSourceRelease(
        IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)> unlocks) =>
        LListRelay.LListSourceRelease(unlocks);

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
