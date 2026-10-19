using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

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
    internal static CSEncoderCodec CSEncoderCodecRead(
        LCapabilityCodec codec, LCapabilityMode mode, LSEncoderPort draft, string rate)
    {
        IReadOnlyList<string> conflicts = mode.LCapabilityModeConflict ?? [];
        LCapabilityQuality? quality = mode.LCapabilityModeQuality;
        LCapabilitySpeed? speed = codec.LCapabilitySpeed;
        return new CSEncoderCodec(
            codec.LCapabilityModeLabels,
            quality is null
                ? null
                : new CSEncoderQuality(quality, draft, rate),
            codec.LCapabilityNotice,
            speed is null
                ? null
                : new CSEncoderSpeed(speed, draft, rate),
            codec.LCapabilityExtraList
                .Where(extra => !conflicts.Contains(extra.LCapabilityExtraOption, StringComparer.Ordinal))
                .Select(extra => new CSEncoderExtra(extra, draft))
                .ToArray());
    }
}
