namespace Cadroue.Core;

public sealed record LWorkOutput(
    string LWorkOutputNamePattern,
    string LWorkOutputContainer,
    string LWorkOutputExtension,
    string LWorkOutputLocation,
    string LWorkOutputLocationFolder,
    string LWorkOutputExportMode,
    string LWorkOutputVideoStream,
    string LWorkOutputVideoMode,
    string LWorkOutputVideoEncoder,
    string LWorkOutputRateControl,
    string LWorkOutputQuality,
    string LWorkOutputSpeedPreset,
    string LWorkOutputVideoSize,
    string LWorkOutputVideoFps,
    string LWorkOutputPixelFormat,
    IReadOnlyDictionary<string, string> LWorkOutputVideoExtras,
    string LWorkOutputAudioStream,
    string LWorkOutputAudioMode,
    string LWorkOutputAudioEncoder,
    string LWorkOutputAudioBitrate,
    string LWorkOutputAudioSampleRate,
    string LWorkOutputAudioChannels);

public sealed record LWorkCrop(
    int LWorkCropLeft,
    int LWorkCropTop,
    int LWorkCropRight,
    int LWorkCropBottom,
    int LWorkCropRotation,
    bool LWorkCropFlipHorizontal,
    bool LWorkCropFlipVertical)
{
    public static LWorkCrop LWorkCropNoneCreate() => new(0, 0, 0, 0, 0, false, false);

    public bool LWorkCropEdgeActive =>
        LWorkCropLeft > 0 || LWorkCropTop > 0 || LWorkCropRight > 0 || LWorkCropBottom > 0;

    public bool LWorkCropActive =>
        LWorkCropEdgeActive
        || LWorkCropRotation != 0
        || LWorkCropFlipHorizontal
        || LWorkCropFlipVertical;
}
