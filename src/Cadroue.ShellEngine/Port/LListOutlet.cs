using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One list's own docket and the folder scan its adds run through. The names the traces show come from the list gate;
// the scan and the trace kinds are the rig's, so the traces stand from the first list on.
public sealed class LListOutlet : LListPort
{
    private readonly LDocket _lListOutletDocket = new();
    private readonly CancellationTokenSource _lListOutletSource = new();
    private readonly Func<string, string> _lListOutletName;
    private readonly LRigList _lListOutletRig;

    public LListOutlet(Func<string, string> lName, LRigList lRig)
    {
        ArgumentNullException.ThrowIfNull(lName);
        ArgumentNullException.ThrowIfNull(lRig);
        _lListOutletName = lName;
        _lListOutletRig = lRig;
        _lListOutletDocket.LDocketChanged += LListChangeHandle;
        _lListOutletDocket.LDocketAdded += LListAddHandle;
        _lListOutletDocket.LDocketDelivered += LListDeliveredHandle;
        _lListOutletDocket.LDocketRemoved += LListRemoveHandle;
    }

    public event Action? LListChanged;

    public event Action<IReadOnlyList<string>>? LListAdded;

    public event Action<IReadOnlyList<LDocketEntry>>? LListEntriesAdded;

    public event Action<IReadOnlyList<string>>? LListRemoved;

    public LDocket LListDocket => _lListOutletDocket;

    public IReadOnlyList<string> LListPathsRead() => _lListOutletDocket.LDocketPathsRead();

    public IReadOnlyList<string> LListUnlockedRead() =>
        _lListOutletDocket.LDocketUnlockedRead()
            .Select(lListItem => lListItem.LDocketEntryPath)
            .ToArray();

    public bool LListPathCheck(string lPath) => _lListOutletDocket.LDocketItemFind(lPath) is not null;

    public bool LListLockCheck(string lPath) => _lListOutletDocket.LDocketLockCheck(lPath);

    public IReadOnlyList<(bool LListLocked, IReadOnlyList<string> LListPaths)> LListCardsRead()
    {
        var lCards = new List<(bool, IReadOnlyList<string>)>();
        var lShown = new HashSet<Guid>();
        IReadOnlyList<LDocketEntry> lEntries = _lListOutletDocket.LDocketItemsRead();
        foreach (LDocketEntry lItem in lEntries)
        {
            if (!lItem.LDocketEntryLocked)
            {
                lCards.Add((false, [lItem.LDocketEntryPath]));
                continue;
            }

            if (lShown.Add(lItem.LDocketEntryBatch))
            {
                lCards.Add((true, LListBatchRead(lEntries, lItem.LDocketEntryBatch)));
            }
        }

        return lCards;
    }

    public async Task<int> LListPathsAdd(IReadOnlyList<string> lPaths)
    {
        _lListOutletRig.LRigListInfo(
            $"List add requested: {lPaths.Count} path(s)",
            string.Join(", ", lPaths.Select(_lListOutletName)));
        try
        {
            (IReadOnlyList<string> lScannedPaths, IReadOnlyList<(string, string)> lSkips) =
                await _lListOutletRig.LRigListScanner(lPaths, _lListOutletSource.Token);
            foreach ((string lFolder, string lReason) in lSkips)
            {
                _lListOutletRig.LRigListWarning($"List skipped folder '{lFolder}': {lReason}", null);
            }

            _lListOutletRig.LRigListInfo(
                $"List scan resolved {lScannedPaths.Count} media path(s); adding to docket", null);
            int lAdded = lScannedPaths.Count == 0 ? 0 : _lListOutletDocket.LDocketPathsAdd(lScannedPaths);
            _lListOutletRig.LRigListInfo($"List add committed: {lAdded} entry(ies)", null);
            return lAdded;
        }
        catch (OperationCanceledException)
        {
            _lListOutletRig.LRigListInfo("List add cancelled: the tab closed during the folder scan", null);
            return 0;
        }
        catch (Exception lAddException)
        {
            _lListOutletRig.LRigListError("List add failed", lAddException);
            return 0;
        }
    }

    public void LListPathsRemove(IReadOnlyList<string> lPaths) => _lListOutletDocket.LDocketPathsRemove(lPaths);

    public void LListClose() => _lListOutletSource.Cancel();

    // The maker of each list's outlet over the rig, handed to the atelier as each list's port.
    public static Func<Func<string, string>, LListPort> LListOutletCreate(LRigList lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);
        return lName => new LListOutlet(lName, lRig);
    }

    // The batch's locked paths in docket order.
    private static IReadOnlyList<string> LListBatchRead(IReadOnlyList<LDocketEntry> lEntries, Guid lBatch) =>
        lEntries
            .Where(lCandidate => lCandidate.LDocketEntryLocked && lCandidate.LDocketEntryBatch == lBatch)
            .Select(lCandidate => lCandidate.LDocketEntryPath)
            .ToArray();

    private void LListChangeHandle(IReadOnlyList<LDocketEntry> lEntries) => LListChanged?.Invoke();

    // The gate selects from the added paths before the entries go on to the readers; both traces frame the two.
    private void LListAddHandle(IReadOnlyList<LDocketEntry> lAdded)
    {
        _lListOutletRig.LRigListInfo(
            $"List add handled: {lAdded.Count} entry(ies), "
            + $"selecting '{_lListOutletName(lAdded[0].LDocketEntryPath)}' and notifying subscribers",
            null);
        LListAdded?.Invoke(lAdded.Select(lEntry => lEntry.LDocketEntryPath).ToArray());
        LListEntriesAdded?.Invoke(lAdded);
        _lListOutletRig.LRigListInfo("List add subscribers notified", null);
    }

    private void LListDeliveredHandle(IReadOnlyList<LDocketEntry> lAdded) => LListEntriesAdded?.Invoke(lAdded);

    private void LListRemoveHandle(IReadOnlyList<string> lRemoved) => LListRemoved?.Invoke(lRemoved);
}
