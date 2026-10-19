using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The video encoder trial over the calls the host hands in: the trial run, the available-set keeper and check,
// and the trace record.
public sealed class LSTrialOutlet : LSTrialPort
{
    private readonly Func<string, CancellationToken, Task<(bool, string)>> _lsTrialProbe;
    private readonly Action<IEnumerable<string>> _lsTrialKeeper;
    private readonly Func<LRepertoireEncoder, bool> _lsTrialFilter;
    private readonly Action<string, string> _lsTrialTrace;

    public LSTrialOutlet(
        Func<string, CancellationToken, Task<(bool, string)>> lProbe,
        Action<IEnumerable<string>> lKeeper,
        Func<LRepertoireEncoder, bool> lFilter,
        Action<string, string> lTrace)
    {
        ArgumentNullException.ThrowIfNull(lProbe);
        ArgumentNullException.ThrowIfNull(lKeeper);
        ArgumentNullException.ThrowIfNull(lFilter);
        ArgumentNullException.ThrowIfNull(lTrace);
        _lsTrialProbe = lProbe;
        _lsTrialKeeper = lKeeper;
        _lsTrialFilter = lFilter;
        _lsTrialTrace = lTrace;
    }

    public async Task<(bool LSTrialSuccess, string LSTrialMessage)> LSTrialRun(
        string lEncoder, CancellationToken lToken) => await _lsTrialProbe(lEncoder, lToken);

    public void LSTrialApply(IEnumerable<string> lAvailable) => _lsTrialKeeper(lAvailable);

    public bool LSTrialCheck(LRepertoireEncoder lCandidate) => _lsTrialFilter(lCandidate);

    public void LSTrialRecord(string lMessage, string lDetail) => _lsTrialTrace(lMessage, lDetail);
}
