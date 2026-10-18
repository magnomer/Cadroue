using Cadroue.Application;

namespace Cadroue.ShellEngine;

// One Application neutral sample for the white balance gate: its status maps by name, and a failed pick is traced
// through the seam the owner hands in.
public sealed class LWhitebalanceSampleOutlet : LWhitebalanceSamplePort
{
    private readonly LNeutralSample _lWhitebalanceSample;
    private readonly Action<string> _lWhitebalanceTrace;

    public LWhitebalanceSampleOutlet(LNeutralSample lSample, Action<string> lTrace)
    {
        ArgumentNullException.ThrowIfNull(lSample);
        ArgumentNullException.ThrowIfNull(lTrace);
        _lWhitebalanceSample = lSample;
        _lWhitebalanceTrace = lTrace;
    }

    public (string LWhitebalanceStatus, double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceSampleRead()
    {
        LNeutralSample lSample = _lWhitebalanceSample;
        string lStatus = LNeutralReadout.LNeutralStatusResolve(lSample.LNeutralOutcome) switch
        {
            LNeutralStatus.LNeutralStatusValid => "Valid",
            LNeutralStatus.LNeutralStatusDecode => "Decode",
            _ => "Invalid"
        };
        if (lStatus == "Decode")
        {
            _lWhitebalanceTrace("Whitebalance pick: frame decode failed");
        }
        else if (lStatus == "Invalid")
        {
            _lWhitebalanceTrace($"Whitebalance pick: invalid sample ({lSample.LNeutralOutcome})");
        }

        return (lStatus, lSample.LNeutralRedGain, lSample.LNeutralGreenGain, lSample.LNeutralBlueGain,
            lSample.LNeutralRed, lSample.LNeutralGreen, lSample.LNeutralBlue);
    }
}
