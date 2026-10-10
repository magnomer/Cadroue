using Cadroue.Core;

namespace Cadroue.Media;

public sealed class LSidecar
{
    public int LSidecarVersion { get; set; } = 2;

    public LSidecarSourceEntry LSidecarSource { get; set; } = new();

    public List<long> LSidecarKeyframeDeltas { get; set; } = new();

    public List<int> LSidecarScannedSpans { get; set; } = new();

    public int LSidecarSpanGrid { get; set; }

    public List<LSidecarSectionEntry> LSidecarSections { get; set; } = new();

    public LSidecarEditEntry? LSidecarEdit { get; set; }

    public LSidecarAudioEntry? LSidecarAudio { get; set; }

    public LSidecarWaveformEntry? LSidecarWaveform { get; set; }

    public double LSidecarLoudness { get; set; }

    public IReadOnlyList<int> LSidecarSpansRead(int lSidecarSpanGridMilliseconds) =>
        LSidecarSpanGrid == lSidecarSpanGridMilliseconds ? LSidecarScannedSpans : Array.Empty<int>();

    public IReadOnlyList<long> LSidecarKeyframesRead() =>
        LSidecarKeyframe.LSidecarKeyframeParse(LSidecarKeyframeDeltas);

    public bool LSidecarSourceMatch(LKeyframeSourceIdentity lSidecarIdentity) =>
        LSidecarSource.LSidecarLength == lSidecarIdentity.LKeyframeSourceLength
        && LSidecarSource.LSidecarWriteTicks == lSidecarIdentity.LKeyframeWriteTicks
        && LSidecarSource.LSidecarDurationMilliseconds == lSidecarIdentity.LKeyframeSourceDuration
        && string.Equals(
            LSidecarSource.LSidecarPartialHash,
            lSidecarIdentity.LKeyframePartialHash,
            StringComparison.Ordinal);
}
