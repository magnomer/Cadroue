using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed record CSEncoderAudioCodec(
    string CSEncoderAudioMode,
    CSCodecQuality? CSEncoderAudioQuality,
    CSCodecSpeed? CSEncoderAudioSpeed,
    IReadOnlyList<(string CSCodecExtraLabel, string CSCodecExtraOption, string CSCodecExtraDefault,
        IReadOnlyList<(string CSEncoderChoiceToken,
            string CSEncoderChoiceLabel)> CSCodecExtraValues)> CSEncoderAudioExtras,
    string CSEncoderAudioCaution);

public sealed record CSCodecQuality(
    string CSCodecQualityLabel,
    string CSCodecQualityDefault,
    double? CSCodecQualityMinimum,
    double? CSCodecQualityMaximum,
    double CSCodecQualityStep,
    bool CSCodecQualityAscending,
    bool CSCodecQualityBitrate,
    string CSCodecQualityKey,
    object?[] CSCodecQualityArguments)
{
    internal static CSCodecQuality CSCodecQualityRead(LCapabilityQuality quality) =>
        new(
            quality.LCapabilityQualityLabel,
            quality.LCapabilityQualityDefault,
            quality.LCapabilityQualityMinimum,
            quality.LCapabilityQualityMaximum,
            quality.LCapabilityQualityStep,
            quality.LCapabilityQualityAscending,
            quality.LCapabilityQualityBitrate,
            string.IsNullOrEmpty(quality.LCapabilityQualityRange)
                ? "Encoder.Audio.FFmpegOption"
                : "Encoder.Audio.FFmpegOptionRange",
            string.IsNullOrEmpty(quality.LCapabilityQualityRange)
                ? [quality.LCapabilityQualityOption]
                : [quality.LCapabilityQualityOption, quality.LCapabilityQualityRange]);
}

public sealed record CSCodecSpeed(
    string CSCodecSpeedLabel,
    string CSCodecSpeedDefault,
    IReadOnlyList<(string CSEncoderChoiceToken, string CSEncoderChoiceLabel)> CSCodecSpeedValues)
{
    internal static CSCodecSpeed CSCodecSpeedRead(LCapabilitySpeed speed) =>
        new(
            speed.LCapabilitySpeedLabel,
            speed.LCapabilitySpeedDefault,
            speed.LCapabilitySpeedValues
                .Select(choice => (choice.LCapabilityChoiceValue, choice.LCapabilityChoiceLabel))
                .ToArray());
}
