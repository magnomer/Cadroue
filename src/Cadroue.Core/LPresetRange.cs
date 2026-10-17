using System.Collections.Generic;

namespace Cadroue.Core;

public static class LPresetRange
{
    public static LPresetDocument LPresetRecordNormalize(LPresetDocument lPresetRecord)
    {
        var lPresetDefault = new LPresetDocument();
        lPresetRecord.LPresetName ??= lPresetDefault.LPresetName;
        lPresetRecord.LPresetDisplay ??= lPresetDefault.LPresetDisplay;
        lPresetRecord.LPresetContainer ??= lPresetDefault.LPresetContainer;
        lPresetRecord.LPresetExtension ??= lPresetDefault.LPresetExtension;
        lPresetRecord.LPresetCollision ??= lPresetDefault.LPresetCollision;
        lPresetRecord.LPresetCollisionSuffix ??= lPresetDefault.LPresetCollisionSuffix;
        lPresetRecord.LPresetOutputSuffix ??= lPresetDefault.LPresetOutputSuffix;
        lPresetRecord.LPresetSourceSuffix ??= lPresetDefault.LPresetSourceSuffix;
        lPresetRecord.LPresetLocation ??= lPresetDefault.LPresetLocation;
        lPresetRecord.LPresetLocationFolder ??= lPresetDefault.LPresetLocationFolder;
        lPresetRecord.LPresetLocationSubfolder ??= lPresetDefault.LPresetLocationSubfolder;
        lPresetRecord.LPresetLocationSibling ??= lPresetDefault.LPresetLocationSibling;
        lPresetRecord.LPresetLocationCustom ??= lPresetDefault.LPresetLocationCustom;
        lPresetRecord.LPresetVideo = LPresetVideoNormalize(lPresetRecord.LPresetVideo ?? new LPresetVideoEntry());
        lPresetRecord.LPresetAudio = LPresetAudioNormalize(lPresetRecord.LPresetAudio ?? new LPresetAudioEntry());
        return lPresetRecord;
    }

    public static LPresetVideoEntry LPresetVideoNormalize(LPresetVideoEntry lPresetVideo)
    {
        var lPresetDefault = new LPresetVideoEntry();
        lPresetVideo.LPresetStream ??= lPresetDefault.LPresetStream;
        lPresetVideo.LPresetMode ??= lPresetDefault.LPresetMode;
        lPresetVideo.LPresetEncoder = LRepertoireVideo.LRepertoireTextNormalize(
            lPresetVideo.LPresetEncoder ?? lPresetDefault.LPresetEncoder);
        lPresetVideo.LPresetRateControl ??= lPresetDefault.LPresetRateControl;
        lPresetVideo.LPresetQuality ??= lPresetDefault.LPresetQuality;
        lPresetVideo.LPresetSpeedPreset ??= lPresetDefault.LPresetSpeedPreset;
        lPresetVideo.LPresetSize ??= lPresetDefault.LPresetSize;
        lPresetVideo.LPresetFps ??= lPresetDefault.LPresetFps;
        lPresetVideo.LPresetPixelLayout ??= lPresetDefault.LPresetPixelLayout;
        lPresetVideo.LPresetExtras = LPresetExtrasNormalize(lPresetVideo.LPresetExtras);
        return lPresetVideo;
    }

    public static LPresetAudioEntry LPresetAudioNormalize(LPresetAudioEntry lPresetAudio)
    {
        var lPresetDefault = new LPresetAudioEntry();
        lPresetAudio.LPresetStream ??= lPresetDefault.LPresetStream;
        lPresetAudio.LPresetMode ??= lPresetDefault.LPresetMode;
        lPresetAudio.LPresetEncoder ??= lPresetDefault.LPresetEncoder;
        lPresetAudio.LPresetRateControl ??= lPresetDefault.LPresetRateControl;
        lPresetAudio.LPresetQuality ??= lPresetDefault.LPresetQuality;
        lPresetAudio.LPresetSpeed ??= lPresetDefault.LPresetSpeed;
        lPresetAudio.LPresetSampleRate ??= lPresetDefault.LPresetSampleRate;
        lPresetAudio.LPresetChannels ??= lPresetDefault.LPresetChannels;
        lPresetAudio.LPresetExtras = LPresetExtrasNormalize(lPresetAudio.LPresetExtras);
        return lPresetAudio;
    }

    private static Dictionary<string, string> LPresetExtrasNormalize(Dictionary<string, string>? lPresetExtras)
    {
        var lPresetNormalized = new Dictionary<string, string>(StringComparer.Ordinal);
        if (lPresetExtras is null)
        {
            return lPresetNormalized;
        }

        foreach ((string lPresetKey, string lPresetValue) in lPresetExtras)
        {
            if (!string.IsNullOrEmpty(lPresetKey))
            {
                lPresetNormalized[lPresetKey] = lPresetValue ?? string.Empty;
            }
        }

        return lPresetNormalized;
    }
}
