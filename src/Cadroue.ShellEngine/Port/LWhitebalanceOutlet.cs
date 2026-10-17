namespace Cadroue.ShellEngine;

// The engine's face for the white balance gate: every member forwards to the same-named member of the engine's white
// balance facade. It rejects a null engine and keeps it for every forward.
public sealed class LWhitebalanceOutlet : LWhitebalancePort
{
    private readonly LEngine _lWhitebalanceOutletEngine;

    public LWhitebalanceOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lWhitebalanceOutletEngine = lEngine;
    }

    public LWhitebalanceAnswer LWhitebalanceCreate(bool lActive) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceCreate(lActive);

    public LWhitebalanceAnswer LWhitebalanceNormalize(LWhitebalanceAnswer lAnswer) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceNormalize(lAnswer);

    public (double LWhitebalanceX, double LWhitebalanceY, bool LWhitebalancePresent) LWhitebalanceWheelRead(
        int lRed, int lGreen, int lBlue) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceWheelRead(lRed, lGreen, lBlue);

    public (double LWhitebalanceLeft, double LWhitebalanceTop, bool LWhitebalancePresent) LWhitebalanceDotRead(
        double lX, double lY, bool lPresent, double lSize, double lDot) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceDotRead(lX, lY, lPresent, lSize, lDot);

    public (bool LWhitebalanceSampled, int LWhitebalanceRed, int LWhitebalanceGreen, int LWhitebalanceBlue)
        LWhitebalanceDisplayRead(bool lManual, int lRed, int lGreen, int lBlue) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceDisplayRead(lManual, lRed, lGreen, lBlue);

    public (double LWhitebalanceRed, double LWhitebalanceGreen, double LWhitebalanceBlue,
        int LWhitebalanceSampleRed, int LWhitebalanceSampleGreen, int LWhitebalanceSampleBlue)
        LWhitebalanceCanvasRead(double lX, double lY, double lSize) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceCanvasRead(lX, lY, lSize);

    public (int LWhitebalanceSize, int LWhitebalanceStride, byte[] LWhitebalancePixels) LWhitebalanceBitmapRead(
        int lSize, double lValue) =>
        _lWhitebalanceOutletEngine.LEngineWhitebalance.LWhitebalanceBitmapRead(lSize, lValue);
}
