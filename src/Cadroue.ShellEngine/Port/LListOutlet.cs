using Cadroue.Application;

namespace Cadroue.ShellEngine;

// One list's own docket and the folder scan its adds run through. The names the traces show come from the list gate;
// the scan is the host's, answering the media paths it found and each folder it skipped with the reason.
public sealed class LListOutlet : LListPort
{
    private readonly LDocket _lListOutletDocket = new();
    private readonly CancellationTokenSource _lListOutletSource = new();
    private readonly Func<string, string> _lListOutletName;
    private readonly Func<IReadOnlyList<string>, CancellationToken,
        Task<(IReadOnlyList<string> LListPaths, IReadOnlyList<(string, string)> LListSkips)>> _lListOutletMedia;

    public LListOutlet(
        Func<string, string> lName,
        Func<IReadOnlyList<string>, CancellationToken,
            Task<(IReadOnlyList<string> LListPaths, IReadOnlyList<(string, string)> LListSkips)>> lMedia)
    {
        ArgumentNullException.ThrowIfNull(lName);
        ArgumentNullException.ThrowIfNull(lMedia);
        _lListOutletName = lName;
        _lListOutletMedia = lMedia;
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

    public async Task<int> LListPathsAdd(IReadOnlyList<string> lPaths)
    {
        LTraceEngine.LTraceDetailSeam(
            false,
            $"List add requested: {lPaths.Count} path(s)",
            string.Join(", ", lPaths.Select(_lListOutletName)));
        try
        {
            (IReadOnlyList<string> lScannedPaths, IReadOnlyList<(string, string)> lSkips) =
                await _lListOutletMedia(lPaths, _lListOutletSource.Token);
            foreach ((string lFolder, string lReason) in lSkips)
            {
                LTraceEngine.LTraceWarningSeam($"List skipped folder '{lFolder}': {lReason}");
            }

            LTraceEngine.LTraceInfoSeam($"List scan resolved {lScannedPaths.Count} media path(s); adding to docket");
            int lAdded = lScannedPaths.Count == 0 ? 0 : _lListOutletDocket.LDocketPathsAdd(lScannedPaths);
            LTraceEngine.LTraceInfoSeam($"List add committed: {lAdded} entry(ies)");
            return lAdded;
        }
        catch (OperationCanceledException)
        {
            LTraceEngine.LTraceInfoSeam("List add cancelled: the tab closed during the folder scan");
            return 0;
        }
        catch (Exception lAddException)
        {
            LTraceEngine.LTraceErrorSeam("List add failed", lAddException);
            return 0;
        }
    }

    public void LListPathsRemove(IReadOnlyList<string> lPaths) => _lListOutletDocket.LDocketPathsRemove(lPaths);

    public void LListClose() => _lListOutletSource.Cancel();

    private void LListChangeHandle(IReadOnlyList<LDocketEntry> lEntries) => LListChanged?.Invoke();

    // The gate selects from the added paths before the entries go on to the readers; both traces frame the two.
    private void LListAddHandle(IReadOnlyList<LDocketEntry> lAdded)
    {
        LTraceEngine.LTraceInfoSeam(
            $"List add handled: {lAdded.Count} entry(ies), "
            + $"selecting '{_lListOutletName(lAdded[0].LDocketEntryPath)}' and notifying subscribers");
        LListAdded?.Invoke(lAdded.Select(lEntry => lEntry.LDocketEntryPath).ToArray());
        LListEntriesAdded?.Invoke(lAdded);
        LTraceEngine.LTraceInfoSeam("List add subscribers notified");
    }

    private void LListDeliveredHandle(IReadOnlyList<LDocketEntry> lAdded) => LListEntriesAdded?.Invoke(lAdded);

    private void LListRemoveHandle(IReadOnlyList<string> lRemoved) => LListRemoved?.Invoke(lRemoved);
}
