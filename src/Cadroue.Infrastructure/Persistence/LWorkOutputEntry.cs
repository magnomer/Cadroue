using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LWorkOutputEntry
{
    public string LWorkNamePattern { get; set; } = string.Empty;
    public string LWorkContainer { get; set; } = "MP4";
    public string LWorkExtension { get; set; } = ".mp4";
    public string LWorkLocation { get; set; } = "Same as source";
    public string LWorkLocationFolder { get; set; } = string.Empty;
    public LWorkVideoEntry LWorkOutputVideo { get; set; } = new();
    public LWorkAudioEntry LWorkOutputAudio { get; set; } = new();
    public string LWorkPresetName { get; set; } = string.Empty;
    public string LWorkCollision { get; set; } = "Overwrite";
    public string LWorkCollisionSuffix { get; set; } = string.Empty;

    public static LWorkOutputEntry LWorkSnapshotCreate(LEncoding lEncoding) => new()
    {
        LWorkNamePattern = lEncoding.LEncodingNamePattern,
        LWorkContainer = lEncoding.LEncodingContainer,
        LWorkExtension = lEncoding.LEncodingExtension,
        LWorkLocation = lEncoding.LEncodingLocation,
        LWorkLocationFolder = lEncoding.LEncodingLocationFolder,
        LWorkOutputVideo = new LWorkVideoEntry
        {
            LWorkStream = lEncoding.LEncodingVideo.LEncodingStream,
            LWorkMode = lEncoding.LEncodingVideo.LEncodingMode,
            LWorkEncoder = lEncoding.LEncodingVideo.LEncodingEncoder,
            LWorkRateControl = lEncoding.LEncodingVideo.LEncodingRateControl,
            LWorkQuality = lEncoding.LEncodingVideo.LEncodingQuality,
            LWorkSpeedPreset = lEncoding.LEncodingVideo.LEncodingSpeedPreset,
            LWorkSize = lEncoding.LEncodingVideo.LEncodingSize,
            LWorkSizeReactive = lEncoding.LEncodingVideo.LEncodingSizeReactive,
            LWorkFps = lEncoding.LEncodingVideo.LEncodingFps,
            LWorkPixelLayout = lEncoding.LEncodingVideo.LEncodingPixel,
            LWorkExtras = new Dictionary<string, string>(
                lEncoding.LEncodingVideo.LEncodingExtras,
                StringComparer.Ordinal)
        },
        LWorkOutputAudio = new LWorkAudioEntry
        {
            LWorkStream = lEncoding.LEncodingAudio.LEncodingStream,
            LWorkMode = lEncoding.LEncodingAudio.LEncodingMode,
            LWorkEncoder = lEncoding.LEncodingAudio.LEncodingEncoder,
            LWorkRateControl = lEncoding.LEncodingAudio.LEncodingRateControl,
            LWorkQuality = lEncoding.LEncodingAudio.LEncodingQuality,
            LWorkSpeed = lEncoding.LEncodingAudio.LEncodingSpeed,
            LWorkExtras = new Dictionary<string, string>(
                lEncoding.LEncodingAudio.LEncodingExtras,
                StringComparer.Ordinal),
            LWorkSampleRate = lEncoding.LEncodingAudio.LEncodingSampleRate,
            LWorkChannels = lEncoding.LEncodingAudio.LEncodingChannels
        },
        LWorkPresetName = lEncoding.LEncodingPresetName,
        LWorkCollision = lEncoding.LEncodingCollision,
        LWorkCollisionSuffix = lEncoding.LEncodingCollisionSuffix
    };

    public void LWorkOutputNormalize()
    {
        LWorkNamePattern ??= string.Empty;
        LWorkContainer ??= "MP4";
        LWorkExtension ??= ".mp4";
        LWorkLocation ??= "Same as source";
        LWorkLocationFolder ??= string.Empty;
        (LWorkOutputVideo ??= new()).LWorkVideoNormalize();
        (LWorkOutputAudio ??= new()).LWorkAudioNormalize();
        LWorkPresetName ??= string.Empty;
        LWorkCollision ??= "Overwrite";
        LWorkCollisionSuffix ??= string.Empty;
    }

    public LEncoding LWorkOutputCreate() => new(
        LWorkNamePattern,
        LWorkContainer,
        LWorkExtension,
        LWorkLocation,
        LWorkLocationFolder,
        new LEncodingVideo(
            LWorkOutputVideo.LWorkStream,
            LWorkOutputVideo.LWorkMode,
            LWorkOutputVideo.LWorkEncoder,
            LWorkOutputVideo.LWorkRateControl,
            LWorkOutputVideo.LWorkQuality,
            LWorkOutputVideo.LWorkSpeedPreset,
            LWorkOutputVideo.LWorkSize,
            LWorkOutputVideo.LWorkSizeReactive,
            LWorkOutputVideo.LWorkFps,
            LWorkOutputVideo.LWorkPixelLayout,
            new Dictionary<string, string>(LWorkOutputVideo.LWorkExtras, StringComparer.Ordinal)),
        new LEncodingAudio(
            LWorkOutputAudio.LWorkStream,
            LWorkOutputAudio.LWorkMode,
            LWorkOutputAudio.LWorkEncoder,
            LWorkOutputAudio.LWorkRateControl,
            LWorkOutputAudio.LWorkQuality,
            LWorkOutputAudio.LWorkSpeed,
            new Dictionary<string, string>(LWorkOutputAudio.LWorkExtras, StringComparer.Ordinal),
            LWorkOutputAudio.LWorkSampleRate,
            LWorkOutputAudio.LWorkChannels),
        LWorkPresetName,
        LWorkCollision,
        LWorkCollisionSuffix);
}

public sealed class LWorkVideoEntry
{
    public string LWorkStream { get; set; } = "Include";
    public string LWorkMode { get; set; } = "Auto";
    public string LWorkEncoder { get; set; } = string.Empty;
    public string LWorkRateControl { get; set; } = string.Empty;
    public string LWorkQuality { get; set; } = string.Empty;
    public string LWorkSpeedPreset { get; set; } = string.Empty;
    public string LWorkSize { get; set; } = "Same as source";
    public bool LWorkSizeReactive { get; set; }
    public string LWorkFps { get; set; } = "Same as source";
    public string LWorkPixelLayout { get; set; } = "Auto";
    public Dictionary<string, string> LWorkExtras { get; set; } = new();

    public void LWorkVideoNormalize()
    {
        LWorkStream ??= "Include";
        LWorkMode = LWorkMode switch
        {
            "Copy" or "Smart" or "Encode" => LWorkMode,
            _ => "Encode"
        };
        LWorkEncoder ??= string.Empty;
        LWorkRateControl ??= string.Empty;
        LWorkQuality ??= string.Empty;
        LWorkSpeedPreset ??= string.Empty;
        LWorkSize ??= "Same as source";
        LWorkFps ??= "Same as source";
        LWorkPixelLayout ??= "Auto";
        LWorkExtras ??= new();
    }
}

public sealed class LWorkAudioEntry
{
    public string LWorkStream { get; set; } = string.Empty;
    public string LWorkMode { get; set; } = "Auto";
    public string LWorkEncoder { get; set; } = string.Empty;
    public string LWorkRateControl { get; set; } = string.Empty;
    public string LWorkQuality { get; set; } = string.Empty;
    public string LWorkSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LWorkExtras { get; set; } = new();
    public string LWorkSampleRate { get; set; } = "Same as source";
    public string LWorkChannels { get; set; } = "Same as source";

    public void LWorkAudioNormalize()
    {
        LWorkStream ??= string.Empty;
        LWorkMode ??= "Auto";
        LWorkEncoder ??= string.Empty;
        LWorkRateControl ??= string.Empty;
        LWorkQuality ??= string.Empty;
        LWorkSpeed ??= string.Empty;
        LWorkExtras ??= new();
        LWorkSampleRate ??= "Same as source";
        LWorkChannels ??= "Same as source";
    }
}
