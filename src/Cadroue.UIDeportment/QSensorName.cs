using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The detector kinds' fixed step names, shared by the processing rows and the inspector's open step.
public static class QSensorName
{
    private const string QSensorBlank = "Blank";
    private const string QSensorScene = "Scene";
    private const string QSensorStill = "Still Image";
    private const string QSensorLuminance = "Luminance";
    private const string QSensorSilence = "Silence";
    private const string QSensorVolume = "Volume";

    public static string QSensorNameRead(LDetectorKind lKind) => lKind switch
    {
        LDetectorKind.LDetectorKindBlank => QSensorBlank,
        LDetectorKind.LDetectorKindScene => QSensorScene,
        LDetectorKind.LDetectorKindStill => QSensorStill,
        LDetectorKind.LDetectorKindLuminance => QSensorLuminance,
        LDetectorKind.LDetectorKindSilence => QSensorSilence,
        LDetectorKind.LDetectorKindVolume => QSensorVolume,
        _ => string.Empty
    };

    public static LDetectorKind? QSensorKindRead(string? lName) => lName switch
    {
        QSensorBlank => LDetectorKind.LDetectorKindBlank,
        QSensorScene => LDetectorKind.LDetectorKindScene,
        QSensorStill => LDetectorKind.LDetectorKindStill,
        QSensorLuminance => LDetectorKind.LDetectorKindLuminance,
        QSensorSilence => LDetectorKind.LDetectorKindSilence,
        QSensorVolume => LDetectorKind.LDetectorKindVolume,
        _ => null
    };
}
