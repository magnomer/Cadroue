using Cadroue.Application;

namespace Cadroue.ShellEngine;

// One list's docket: each read the list gate decides from, each edit with the trace it leaves, and every change the
// docket tells, in the order the gate needs it.
public interface LListPort
{
    event Action? LListChanged;

    // The paths one add put on the docket, in docket order; told before LListEntriesAdded.
    event Action<IReadOnlyList<string>>? LListAdded;

    // The entries an add or a delivery put on the docket, passed through for the readers not yet moved to a gate.
    event Action<IReadOnlyList<LDocketEntry>>? LListEntriesAdded;

    event Action<IReadOnlyList<string>>? LListRemoved;

    // The docket itself, only for the readers not yet moved to a gate.
    LDocket LListDocket { get; }

    IReadOnlyList<string> LListPathsRead();

    IReadOnlyList<string> LListUnlockedRead();

    // Whether the docket holds this path.
    bool LListPathCheck(string lPath);

    bool LListLockCheck(string lPath);

    // Scans the paths for media and adds what it finds; answers the count added, 0 when the scan is cancelled or fails.
    Task<int> LListPathsAdd(IReadOnlyList<string> lPaths);

    void LListPathsRemove(IReadOnlyList<string> lPaths);

    // Cancels a running scan: the list's tab closed.
    void LListClose();
}
