using Cadroue.Application;

namespace Cadroue.ShellEngine;

// One checkup result for the clinic gate, read as plain values. A flaw kind travels by its clinic name.
public sealed class LCheckupOutlet : LCheckupPort
{
    private readonly LCheckupResult _lCheckupResult;

    public LCheckupOutlet(LCheckupResult lResult)
    {
        _lCheckupResult = lResult;
    }

    public (string LCheckupSource, string LCheckupKind, bool LCheckupScanning) LCheckupRead() =>
        (_lCheckupResult.LCheckupSource,
            LClinicFlaw.LClinicNameRead(_lCheckupResult.LCheckupKind),
            _lCheckupResult.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeScanning);

    // The wording runs untested, scanning, clean, failed, then the defect, evidence and repair labels.
    public string LCheckupFormat(IReadOnlyList<string> lWording)
    {
        ArgumentNullException.ThrowIfNull(lWording);
        return LCheckupFormatter.LCheckupBodyFormat(
            _lCheckupResult,
            new LCheckupStrings(
                lWording[0], lWording[1], lWording[2], lWording[3], lWording[4], lWording[5], lWording[6]));
    }
}
