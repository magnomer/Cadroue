namespace Cadroue.Core;

public sealed record LEncoding(
    string LEncodingNamePattern,
    string LEncodingContainer,
    string LEncodingExtension,
    string LEncodingLocation,
    string LEncodingLocationFolder,
    LEncodingVideo LEncodingVideo,
    LEncodingAudio LEncodingAudio,
    string LEncodingPresetName,
    string LEncodingCollision,
    string LEncodingCollisionSuffix)
{
    public bool LEncodingSupportCheck(LWorkKind lEncodingKind) =>
        LEncodingSupportCheck(
            lEncodingKind,
            LEncodingVideo.LEncodingMode,
            LEncodingAudio.LEncodingMode,
            LEncodingAudio.LEncodingStream);

    public static bool LEncodingSupportCheck(
        LWorkKind lEncodingKind, string lEncodingVideoMode, string lEncodingAudioMode, string lEncodingAudioStream) =>
        lEncodingKind switch
        {
            LWorkKind.LWorkKindEdit => !string.Equals(lEncodingVideoMode, "Copy", StringComparison.OrdinalIgnoreCase),
            LWorkKind.LWorkKindAudio =>
                !string.Equals(lEncodingAudioMode, "Copy", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(lEncodingAudioMode, "Exclude", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(lEncodingAudioStream, "Exclude", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
}

public sealed record LEncodingVideo(
    string LEncodingStream,
    string LEncodingMode,
    string LEncodingEncoder,
    string LEncodingRateControl,
    string LEncodingQuality,
    string LEncodingSpeedPreset,
    string LEncodingSize,
    bool LEncodingSizeReactive,
    string LEncodingFps,
    string LEncodingPixel,
    IReadOnlyDictionary<string, string> LEncodingExtras);

public sealed record LEncodingAudio(
    string LEncodingStream,
    string LEncodingMode,
    string LEncodingEncoder,
    string LEncodingRateControl,
    string LEncodingQuality,
    string LEncodingSpeed,
    IReadOnlyDictionary<string, string> LEncodingExtras,
    string LEncodingSampleRate,
    string LEncodingChannels);
