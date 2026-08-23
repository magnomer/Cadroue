using System.Collections.Generic;

namespace Cadroue.Core;

public sealed class LSidecarCoreRecord
{
    public int LSidecarVersion { get; set; } = 2;
    public LSidecarSourceRecord LSidecarSource { get; set; } = new();
    public List<LSidecarSectionRecord> LSidecarSections { get; set; } = new();
    public LSidecarEditRecord? LSidecarEdit { get; set; }
    public LSidecarAudioRecord? LSidecarAudio { get; set; }
    public LSidecarSplitRecord? LSidecarSplit { get; set; }
    public double LSidecarLoudness { get; set; }
}

public enum LSidecarSourceKind
{
    LSidecarSourceSibling,
    LSidecarSourceRelative,
    LSidecarSourceAbsolute,
    LSidecarSourceMissing
}

public sealed record LSidecarSourceResult(
    string LSidecarResultPath,
    LSidecarSourceKind LSidecarResultKind,
    bool LSidecarResultVerified,
    string LSidecarResultName);

public sealed class LSidecarCacheRecord
{
    public int LSidecarVersion { get; set; } = 2;
    public long LSidecarLength { get; set; }
    public string LSidecarPartialHash { get; set; } = string.Empty;
    public int LSidecarSpanGrid { get; set; }
    public int LSidecarKeyframeCount { get; set; }
    public long LSidecarKeyframeLast { get; set; }
    public List<int> LSidecarScannedSpans { get; set; } = new();
    public List<long> LSidecarKeyframeDeltas { get; set; } = new();
    public LSidecarWaveformRecord? LSidecarWaveform { get; set; }
}
