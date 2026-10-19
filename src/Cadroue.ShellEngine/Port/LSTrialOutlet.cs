using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The video encoder trial over the calls the host hands in: the trial run, the available-set keeper and check,
// and the trace record.
public sealed class LSTrialOutlet : LSTrialPort
{
    private readonly Func<string, CancellationToken, Task<(bool, string)>> _lsTrialProbe;
    private readonly Action<IEnumerable<string>> _lsTrialKeeper;
    private readonly Func<LRepertoireEncoder, bool> _lsTrialFilter;
    private readonly Func<string, CancellationToken, Task<(bool, string)>> _lsTrialAudioProbe;
    private readonly Action<IEnumerable<string>> _lsTrialAudioKeeper;
    private readonly Func<string, bool> _lsTrialAudioFilter;
    private readonly Action<string, string> _lsTrialTrace;

    public LSTrialOutlet(
        Func<string, CancellationToken, Task<(bool, string)>> lProbe,
        Action<IEnumerable<string>> lKeeper,
        Func<LRepertoireEncoder, bool> lFilter,
        Func<string, CancellationToken, Task<(bool, string)>> lAudioProbe,
        Action<IEnumerable<string>> lAudioKeeper,
        Func<string, bool> lAudioFilter,
        Action<string, string> lTrace)
    {
        ArgumentNullException.ThrowIfNull(lProbe);
        ArgumentNullException.ThrowIfNull(lKeeper);
        ArgumentNullException.ThrowIfNull(lFilter);
        ArgumentNullException.ThrowIfNull(lAudioProbe);
        ArgumentNullException.ThrowIfNull(lAudioKeeper);
        ArgumentNullException.ThrowIfNull(lAudioFilter);
        ArgumentNullException.ThrowIfNull(lTrace);
        _lsTrialProbe = lProbe;
        _lsTrialKeeper = lKeeper;
        _lsTrialFilter = lFilter;
        _lsTrialAudioProbe = lAudioProbe;
        _lsTrialAudioKeeper = lAudioKeeper;
        _lsTrialAudioFilter = lAudioFilter;
        _lsTrialTrace = lTrace;
    }

    public async Task<(bool LSTrialSuccess, string LSTrialMessage)> LSTrialRun(
        string lEncoder, CancellationToken lToken) => await _lsTrialProbe(lEncoder, lToken);

    public void LSTrialApply(IEnumerable<string> lAvailable) => _lsTrialKeeper(lAvailable);

    public bool LSTrialCheck(LRepertoireEncoder lCandidate) => _lsTrialFilter(lCandidate);

    public async Task<(bool LSTrialSuccess, string LSTrialMessage)> LSTrialAudioRun(
        string lName, CancellationToken lToken) => await _lsTrialAudioProbe(lName, lToken);

    public void LSTrialAudioApply(IEnumerable<string> lAvailable) => _lsTrialAudioKeeper(lAvailable);

    public bool LSTrialAudioCheck(string lName) => _lsTrialAudioFilter(lName);

    public void LSTrialRecord(string lMessage, string lDetail) => _lsTrialTrace(lMessage, lDetail);
}
