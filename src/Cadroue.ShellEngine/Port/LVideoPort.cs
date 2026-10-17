namespace Cadroue.ShellEngine;

// The video plan rules for the inspector gate. Each tool arrives as its plain values, or null when the gate leaves it
// out of the plan; gamma values run in the gamma gate's slot order, and curve points run per channel as input and
// output. A plan in the caller's order keeps a step for each processing step token that names a tool the gate put in;
// the persistent plan runs in the tools' order.
public interface LVideoPort
{
    LInspectorPort LVideoPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve);

    LInspectorPort LVideoPersistentCreate(
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve);
}
