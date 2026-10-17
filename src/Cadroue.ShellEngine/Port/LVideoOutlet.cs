namespace Cadroue.ShellEngine;

// The engine's face for the video plans: every member forwards to the same-named member of the engine's video
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LVideoOutlet : LVideoPort
{
    private readonly LEngine _lVideoOutletEngine;

    public LVideoOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lVideoOutletEngine = lEngine;
    }

    public LInspectorPort LVideoPlanCreate(
        IReadOnlyList<string> lProcessing,
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve) =>
        _lVideoOutletEngine.LEngineVideo.LVideoPlanCreate(
            lProcessing, lBrightness, lContrast, lSaturation, lGamma, lWhitebalance, lExposure, lCurve);

    public LInspectorPort LVideoPersistentCreate(
        (bool LVideoActive, double LVideoValue)? lBrightness,
        (bool LVideoActive, double LVideoValue)? lContrast,
        (bool LVideoActive, double LVideoValue)? lSaturation,
        (bool LVideoActive, IReadOnlyList<double> LVideoValues)? lGamma,
        LWhitebalanceAnswer? lWhitebalance,
        (bool LVideoActive, double LVideoValue)? lExposure,
        (bool LVideoActive, IReadOnlyList<IReadOnlyList<(double, double)>> LVideoChannels)? lCurve) =>
        _lVideoOutletEngine.LEngineVideo.LVideoPersistentCreate(
            lBrightness, lContrast, lSaturation, lGamma, lWhitebalance, lExposure, lCurve);
}
