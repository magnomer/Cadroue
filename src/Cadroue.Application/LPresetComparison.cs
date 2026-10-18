using Cadroue.Core;

namespace Cadroue.Application;

// Whether two presets, or two stored preset documents, hold the same values; the name is not compared.
internal static class LPresetComparison
{
    internal static bool LPresetValueMatch(LPreset lFirstPreset, LPreset lSecondPreset)
    {
        return string.Equals(lFirstPreset.LPresetDisplay, lSecondPreset.LPresetDisplay, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetContainer, lSecondPreset.LPresetContainer, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetExtension, lSecondPreset.LPresetExtension, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetCollision, lSecondPreset.LPresetCollision, StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetOutputSuffix,
                lSecondPreset.LPresetOutputSuffix,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetSourceSuffix,
                lSecondPreset.LPresetSourceSuffix,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetStream,
                lSecondPreset.LPresetVideo.LPresetStream,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetStream,
                lSecondPreset.LPresetAudio.LPresetStream,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetMode,
                lSecondPreset.LPresetVideo.LPresetMode,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetMode,
                lSecondPreset.LPresetAudio.LPresetMode,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetEncoder,
                lSecondPreset.LPresetVideo.LPresetEncoder,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetRateControl,
                lSecondPreset.LPresetVideo.LPresetRateControl,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetQuality,
                lSecondPreset.LPresetVideo.LPresetQuality,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetSpeedPreset,
                lSecondPreset.LPresetVideo.LPresetSpeedPreset,
                StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetLocation, lSecondPreset.LPresetLocation, StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetLocationSubfolder,
                lSecondPreset.LPresetLocationSubfolder,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetLocationSibling,
                lSecondPreset.LPresetLocationSibling,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetLocationCustom,
                lSecondPreset.LPresetLocationCustom,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetSize,
                lSecondPreset.LPresetVideo.LPresetSize,
                StringComparison.Ordinal)
            && lFirstPreset.LPresetVideo.LPresetSizeReactive == lSecondPreset.LPresetVideo.LPresetSizeReactive
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetFps,
                lSecondPreset.LPresetVideo.LPresetFps,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetVideo.LPresetPixelLayout,
                lSecondPreset.LPresetVideo.LPresetPixelLayout,
                StringComparison.Ordinal)
            && LPresetExtraMatch(lFirstPreset.LPresetVideo.LPresetExtras, lSecondPreset.LPresetVideo.LPresetExtras)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetEncoder,
                lSecondPreset.LPresetAudio.LPresetEncoder,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetRateControl,
                lSecondPreset.LPresetAudio.LPresetRateControl,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetQuality,
                lSecondPreset.LPresetAudio.LPresetQuality,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetSpeed,
                lSecondPreset.LPresetAudio.LPresetSpeed,
                StringComparison.Ordinal)
            && LPresetExtraMatch(lFirstPreset.LPresetAudio.LPresetExtras, lSecondPreset.LPresetAudio.LPresetExtras)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetSampleRate,
                lSecondPreset.LPresetAudio.LPresetSampleRate,
                StringComparison.Ordinal)
            && string.Equals(
                lFirstPreset.LPresetAudio.LPresetChannels,
                lSecondPreset.LPresetAudio.LPresetChannels,
                StringComparison.Ordinal);
    }

    private static bool LPresetExtraMatch(
        IReadOnlyDictionary<string, string> lFirstExtras,
        IReadOnlyDictionary<string, string> lSecondExtras)
    {
        if (lFirstExtras.Count != lSecondExtras.Count)
        {
            return false;
        }

        foreach ((string lKey, string lValue) in lFirstExtras)
        {
            if (!lSecondExtras.TryGetValue(lKey, out string? lSecondValue)
                || !string.Equals(lValue, lSecondValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool LPresetRecordMatch(LPresetDocument lFirstRecord, LPresetDocument lSecondRecord) =>
        LPresetValueMatch(LPreset.LPresetStateCreate(lFirstRecord), LPreset.LPresetStateCreate(lSecondRecord));
}
