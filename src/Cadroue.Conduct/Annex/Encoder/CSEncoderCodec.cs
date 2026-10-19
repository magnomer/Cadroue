using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Conduct;

// One value and the label its row shows.
public sealed record CSEncoderChoice(string CSEncoderChoiceValue, string CSEncoderChoiceLabel)
{
    internal static CSEncoderChoice CSEncoderChoiceRead(LCapabilityChoice choice) =>
        new(choice.LCapabilityChoiceValue, choice.LCapabilityChoiceLabel);
}

// The kept mode's quality entry: its label and option, its default, its range and step, whether higher is better,
// whether it is a bitrate, and the range text.
public sealed record CSEncoderQuality(
    string CSEncoderQualityLabel,
    string CSEncoderQualityOption,
    string CSEncoderQualityDefault,
    double? CSEncoderQualityMinimum,
    double? CSEncoderQualityMaximum,
    bool CSEncoderQualityAscending,
    double CSEncoderQualityStep,
    bool CSEncoderQualityBitrate,
    string CSEncoderQualityRange);

// The codec's speed row: its label, its default value and the values it offers.
public sealed record CSEncoderSpeed(
    string CSEncoderSpeedLabel,
    string CSEncoderSpeedDefault,
    IReadOnlyList<CSEncoderChoice> CSEncoderSpeedValues);

// One extra option row: its label and option, its default value and the values it offers.
public sealed record CSEncoderExtra(
    string CSEncoderExtraLabel,
    string CSEncoderExtraOption,
    string CSEncoderExtraDefault,
    IReadOnlyList<CSEncoderChoice> CSEncoderExtraValues);

// What the video rate rows read for one encoder at one rate: the codec's rate modes, the kept mode's quality, the
// codec's notice and speed, and the extras the kept mode leaves open.
public sealed record CSEncoderCodec(
    IReadOnlyList<string> CSEncoderCodecModes,
    CSEncoderQuality? CSEncoderCodecQuality,
    string CSEncoderCodecNotice,
    CSEncoderSpeed? CSEncoderCodecSpeed,
    IReadOnlyList<CSEncoderExtra> CSEncoderCodecExtras)
{
    // The kept mode leaves open every extra its conflicts do not name.
    internal static CSEncoderCodec CSEncoderCodecRead(LCapabilityCodec codec, LCapabilityMode mode)
    {
        IReadOnlyList<string> conflicts = mode.LCapabilityModeConflict ?? [];
        LCapabilityQuality? quality = mode.LCapabilityModeQuality;
        LCapabilitySpeed? speed = codec.LCapabilitySpeed;
        return new CSEncoderCodec(
            codec.LCapabilityModeLabels,
            quality is null
                ? null
                : new CSEncoderQuality(
                    quality.LCapabilityQualityLabel,
                    quality.LCapabilityQualityOption,
                    quality.LCapabilityQualityDefault,
                    quality.LCapabilityQualityMinimum,
                    quality.LCapabilityQualityMaximum,
                    quality.LCapabilityQualityAscending,
                    quality.LCapabilityQualityStep,
                    quality.LCapabilityQualityBitrate,
                    quality.LCapabilityQualityRange),
            codec.LCapabilityNotice,
            speed is null
                ? null
                : new CSEncoderSpeed(
                    speed.LCapabilitySpeedLabel,
                    speed.LCapabilitySpeedDefault,
                    speed.LCapabilitySpeedValues.Select(CSEncoderChoice.CSEncoderChoiceRead).ToArray()),
            codec.LCapabilityExtraList
                .Where(extra => !conflicts.Contains(extra.LCapabilityExtraOption, StringComparer.Ordinal))
                .Select(extra => new CSEncoderExtra(
                    extra.LCapabilityExtraLabel,
                    extra.LCapabilityExtraOption,
                    extra.LCapabilityExtraDefault,
                    extra.LCapabilityExtraValues.Select(CSEncoderChoice.CSEncoderChoiceRead).ToArray()))
                .ToArray());
    }
}
