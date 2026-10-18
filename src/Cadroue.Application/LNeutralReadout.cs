namespace Cadroue.Application;

// What the neutral readout shows: the status a sample outcome reads as, and the swatch a picked colour displays.
public static class LNeutralReadout
{
    public static LNeutralStatus LNeutralStatusResolve(LNeutralOutcome lNeutralOutcome) => lNeutralOutcome switch
    {
        LNeutralOutcome.LNeutralOutcomeResolved => LNeutralStatus.LNeutralStatusValid,
        LNeutralOutcome.LNeutralOutcomeDecode => LNeutralStatus.LNeutralStatusDecode,
        _ => LNeutralStatus.LNeutralStatusInvalid
    };

    public static LNeutralDisplay LNeutralDisplayResolve(
        bool lNeutralManual,
        int lNeutralRed,
        int lNeutralGreen,
        int lNeutralBlue)
    {
        int lNeutralClampedRed = Math.Clamp(lNeutralRed, 0, 255);
        int lNeutralClampedGreen = Math.Clamp(lNeutralGreen, 0, 255);
        int lNeutralClampedBlue = Math.Clamp(lNeutralBlue, 0, 255);
        bool lNeutralSampled = lNeutralManual
            && (lNeutralClampedRed | lNeutralClampedGreen | lNeutralClampedBlue) != 0;
        return lNeutralSampled
            ? new LNeutralDisplay(
                true, true, lNeutralClampedRed, lNeutralClampedGreen, lNeutralClampedBlue)
            : new LNeutralDisplay(lNeutralManual, false, 0, 0, 0);
    }
}
