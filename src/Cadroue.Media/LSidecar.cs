using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Media;

public sealed class LSidecarSectionRecord
{
    public long LSidecarStartMilliseconds { get; set; }
    public long LSidecarEndMilliseconds { get; set; }
    public int LSidecarColorIndex { get; set; }
    public string LSidecarName { get; set; } = string.Empty;
    public string LSidecarPrefix { get; set; } = string.Empty;
    public string LSidecarSuffix { get; set; } = string.Empty;
    public bool LSidecarHidden { get; set; }
}

public sealed class LSidecarWaveformRecord
{
    public int LSidecarBucketMilliseconds { get; set; }
    public long LSidecarDurationMilliseconds { get; set; }
    public string LSidecarPeaks { get; set; } = string.Empty;
    public string LSidecarRms { get; set; } = string.Empty;
}

public sealed class LSidecarSourceRecord
{
    public string LSidecarFileName { get; set; } = string.Empty;
    public string LSidecarRelativePath { get; set; } = string.Empty;
    public string LSidecarAbsolutePath { get; set; } = string.Empty;
    public long LSidecarLength { get; set; }
    public long LSidecarWriteTicks { get; set; }
    public long LSidecarDurationMilliseconds { get; set; }
    public string LSidecarPartialHash { get; set; } = string.Empty;
}

public sealed class LSidecar
{
    public const string LSidecarExtension = ".cad";

    public int LSidecarVersion { get; set; } = 1;

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

    public static LSidecar LSidecarCreate(
        LKeyframeSourceIdentity lSidecarIdentity,
        string lSidecarFilePath,
        IReadOnlyCollection<long> lSidecarKeyframeMilliseconds,
        IReadOnlyCollection<int> lSidecarScannedSpans,
        int lSidecarSpanGridMilliseconds,
        IReadOnlyList<LSidecarSectionRecord> lSidecarSections)
    {
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarFilePath)) ?? string.Empty;
        string lSidecarSourcePath = lSidecarIdentity.LKeyframeSourcePath;

        return new LSidecar
        {
            LSidecarSource = new LSidecarSourceRecord
            {
                LSidecarFileName = Path.GetFileName(lSidecarSourcePath),
                LSidecarRelativePath = LSidecarRelativeCreate(lSidecarFolder, lSidecarSourcePath),
                LSidecarAbsolutePath = lSidecarSourcePath,
                LSidecarLength = lSidecarIdentity.LKeyframeSourceLength,
                LSidecarWriteTicks = lSidecarIdentity.LKeyframeWriteTicks,
                LSidecarDurationMilliseconds = lSidecarIdentity.LKeyframeSourceDuration,
                LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash
            },
            LSidecarKeyframeDeltas = LSidecarKeyframeFormat(lSidecarKeyframeMilliseconds),
            LSidecarScannedSpans = lSidecarScannedSpans.Where(lSpan => lSpan >= 0).Distinct().Order().ToList(),
            LSidecarSpanGrid = lSidecarSpanGridMilliseconds,
            LSidecarSections = lSidecarSections.ToList()
        };
    }

    public static List<long> LSidecarKeyframeFormat(IReadOnlyCollection<long> lSidecarKeyframeMilliseconds)
    {
        var lSidecarDeltas = new List<long>(lSidecarKeyframeMilliseconds.Count);
        long lSidecarPrevious = 0;
        foreach (long lSidecarKeyframe in lSidecarKeyframeMilliseconds
                     .Where(lKeyframe => lKeyframe >= 0)
                     .Distinct()
                     .OrderBy(lKeyframe => lKeyframe))
        {
            lSidecarDeltas.Add(lSidecarKeyframe - lSidecarPrevious);
            lSidecarPrevious = lSidecarKeyframe;
        }

        return lSidecarDeltas;
    }

    public static IReadOnlyList<long> LSidecarKeyframeParse(IReadOnlyList<long> lSidecarDeltas)
    {
        var lSidecarKeyframes = new List<long>(lSidecarDeltas.Count);
        long lSidecarRunning = 0;
        foreach (long lSidecarDelta in lSidecarDeltas)
        {
            lSidecarRunning += lSidecarDelta;
            if (lSidecarRunning >= 0)
            {
                lSidecarKeyframes.Add(lSidecarRunning);
            }
        }

        return lSidecarKeyframes;
    }

    public IReadOnlyList<long> LSidecarKeyframesRead() => LSidecarKeyframeParse(LSidecarKeyframeDeltas);

    public bool LSidecarSourceMatch(LKeyframeSourceIdentity lSidecarIdentity) =>
        LSidecarSource.LSidecarLength == lSidecarIdentity.LKeyframeSourceLength
        && string.Equals(LSidecarSource.LSidecarPartialHash, lSidecarIdentity.LKeyframePartialHash, StringComparison.Ordinal);

    public string LSidecarJsonCreate() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });

    public static LSidecar? LSidecarParse(string lSidecarJson)
    {
        try
        {
            LSidecar? lSidecar = JsonSerializer.Deserialize<LSidecar>(lSidecarJson);
            lSidecar?.LSidecarNormalize();
            return lSidecar;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void LSidecarNormalize()
    {
        LSidecarSource ??= new();
        LSidecarSource.LSidecarFileName ??= string.Empty;
        LSidecarSource.LSidecarRelativePath ??= string.Empty;
        LSidecarSource.LSidecarAbsolutePath ??= string.Empty;
        LSidecarSource.LSidecarPartialHash ??= string.Empty;
        LSidecarKeyframeDeltas ??= new();
        LSidecarScannedSpans ??= new();
        LSidecarSections ??= new();

        if (LSidecarEdit is { } lSidecarEdit)
        {
            lSidecarEdit.LSidecarSteps ??= new();
        }

        if (LSidecarAudio is { } lSidecarAudio)
        {
            lSidecarAudio.LSidecarSteps ??= new();
        }

        if (LSidecarWaveform is { } lSidecarWaveform)
        {
            lSidecarWaveform.LSidecarPeaks ??= string.Empty;
            lSidecarWaveform.LSidecarRms ??= string.Empty;
        }
    }

    private static string LSidecarRelativeCreate(string lSidecarFolder, string lSidecarSourcePath)
    {
        if (string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetRelativePath(lSidecarFolder, lSidecarSourcePath);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
}
