namespace Cadroue.ShellEngine;

// The placement trace texts, and the preference read the host hands in.
public sealed class LPlacementOutlet : LPlacementPort
{
    private readonly Func<IReadOnlyList<(string, string)>, IEnumerable<(string, double, double, double, double)>>
        _lPlacementOutletPreference;

    public LPlacementOutlet(
        Func<IReadOnlyList<(string, string)>, IEnumerable<(string, double, double, double, double)>> lPreference)
    {
        ArgumentNullException.ThrowIfNull(lPreference);
        _lPlacementOutletPreference = lPreference;
    }

    public IEnumerable<(string, double, double, double, double)> LPlacementPreferenceRead(
        IReadOnlyList<(string, string)> lEntries) => _lPlacementOutletPreference(lEntries);

    public void LPlacementUnreadableRecord(string lReason) =>
        LTraceEngine.LTraceErrorSeam($"Subwindow placement catalogue is unreadable: {lReason}", null);

    public void LPlacementBlockedRecord(string lKey) =>
        LTraceEngine.LTraceErrorSeam(
            $"Subwindow placement not saved because its catalogue is unreadable: {lKey}", null);

    public void LPlacementUnsavedRecord(string lKey) =>
        LTraceEngine.LTraceErrorSeam($"Subwindow placement could not be saved: {lKey}", null);

    public void LPlacementCarriedRecord(string lKey) =>
        LTraceEngine.LTraceInfoSeam($"Subwindow placement carried from preferences: {lKey}");

    public void LPlacementUncarriedRecord(Exception lFailure) =>
        LTraceEngine.LTraceErrorSeam("Subwindow placement could not be carried from preferences", lFailure);
}
