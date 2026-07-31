using System.Text.Json;

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

public sealed class LSidecarEditRecord
{
    public int LSidecarCropLeft { get; set; }
    public int LSidecarCropTop { get; set; }
    public int LSidecarCropRight { get; set; }
    public int LSidecarCropBottom { get; set; }
    public int LSidecarRotation { get; set; }
    public bool LSidecarFlipHorizontal { get; set; }
    public bool LSidecarFlipVertical { get; set; }
    public bool LSidecarCropActive { get; set; }
    public bool LSidecarSkip { get; set; }
    public List<LSidecarVideoStepRecord> LSidecarSteps { get; set; } = new();

    public bool LSidecarEditActive =>
        LSidecarCropActive
        || LSidecarSkip
        || LSidecarCropLeft > 0 || LSidecarCropTop > 0 || LSidecarCropRight > 0 || LSidecarCropBottom > 0
        || LSidecarRotation != 0 || LSidecarFlipHorizontal || LSidecarFlipVertical
        || LSidecarSteps.Any(lStep => lStep.LSidecarActive);
}

public sealed class LSidecarVideoStepRecord
{
    public string LSidecarKind { get; set; } = string.Empty;
    public bool LSidecarActive { get; set; }
    public double LSidecarValue { get; set; }
}

public sealed class LSidecarAudioStepRecord
{
    public string LSidecarKind { get; set; } = string.Empty;
    public bool LSidecarActive { get; set; }
    public double LSidecarGain { get; set; }
    public string LSidecarMode { get; set; } = "Loudness";
    public double LSidecarTarget { get; set; } = -16;
    public double LSidecarPeak { get; set; } = -1.5;
    public double LSidecarRange { get; set; } = 11;
    public bool LSidecarTwoPass { get; set; }
    public double LSidecarReduction { get; set; } = 12;
    public double LSidecarNoiseFloor { get; set; } = -50;
    public bool LSidecarTrackNoise { get; set; }
    public double LSidecarFrequency { get; set; }
    public int LSidecarStages { get; set; } = 1;
    public int LSidecarPoles { get; set; } = 2;
    public double LSidecarResonance { get; set; } = 0.707;
    public string LSidecarNoiseType { get; set; } = "White";
    public double LSidecarGainSmooth { get; set; }
    public double LSidecarAdaptivity { get; set; } = 0.5;
    public double LSidecarResidualFloor { get; set; } = -38;
}

public sealed class LSidecarAudioRecord
{
    public bool LSidecarSkip { get; set; }
    public List<LSidecarAudioStepRecord> LSidecarSteps { get; set; } = new();

    public bool LSidecarAudioActive => LSidecarSkip || LSidecarSteps.Any(lStep => lStep.LSidecarActive);
}

public sealed class LSidecarWaveformRecord
{
    public int LSidecarBucketMilliseconds { get; set; }
    public long LSidecarDurationMilliseconds { get; set; }
    public string LSidecarPeaks { get; set; } = string.Empty;
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
