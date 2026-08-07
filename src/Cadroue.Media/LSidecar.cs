using Cadroue.Core;

namespace Cadroue.Media;

public sealed class LSidecar
{
    public int LSidecarVersion { get; set; } = 2;

    public LSidecarSourceRecord LSidecarSource { get; set; } = new();

    public List<long> LSidecarKeyframeDeltas { get; set; } = new();

    public List<int> LSidecarScannedSpans { get; set; } = new();

    public int LSidecarSpanGrid { get; set; }

    public List<LSidecarSectionRecord> LSidecarSections { get; set; } = new();

    public LSidecarEditRecord? LSidecarEdit { get; set; }

    public LSidecarAudioRecord? LSidecarAudio { get; set; }

    public LSidecarWaveformRecord? LSidecarWaveform { get; set; }

    public double LSidecarLoudness { get; set; }

    public IReadOnlyList<int> LSidecarSpansRead(int lSidecarSpanGridMilliseconds) =>
        LSidecarSpanGrid == lSidecarSpanGridMilliseconds ? LSidecarScannedSpans : Array.Empty<int>();

    public IReadOnlyList<long> LSidecarKeyframesRead() =>
        LSidecarKeyframe.LSidecarKeyframeParse(LSidecarKeyframeDeltas);

    public bool LSidecarSourceMatch(LKeyframeSourceIdentity lSidecarIdentity) =>
        LSidecarSource.LSidecarLength == lSidecarIdentity.LKeyframeSourceLength
        && string.Equals(LSidecarSource.LSidecarPartialHash, lSidecarIdentity.LKeyframePartialHash, StringComparison.Ordinal);
}
