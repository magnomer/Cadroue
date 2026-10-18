using System;

namespace Cadroue.Conduct;

// The info panel decisions: when the media availability probe is asked for and when its answer changes the panel.
// The driver hands in the shown cargo's path, null when no cargo is shown, and its reactions; it never tests an answer.
public sealed class CInfo
{
    internal CInfo()
    {
    }

    // A pathless cargo asks for the probe while no availability answer is known.
    public void CInfoProbeDefer(string? sourcePath, bool? availability, Action defer)
    {
        ArgumentNullException.ThrowIfNull(defer);
        if (CInfoProbeCheck(sourcePath) && availability is null)
        {
            defer();
        }
    }

    // The probe's answer changes the panel only while it shows a pathless cargo.
    public void CInfoAvailabilityPropagate(string? sourcePath, Action changed)
    {
        ArgumentNullException.ThrowIfNull(changed);
        if (CInfoProbeCheck(sourcePath))
        {
            changed();
        }
    }

    internal static bool CInfoProbeCheck(string? sourcePath) => sourcePath is { Length: 0 };
}
