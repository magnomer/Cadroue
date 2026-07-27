using System.Text.Json;

namespace Cadroue.Media;

public sealed class LSidecarSectionRecord
{
    public long StartMilliseconds { get; set; }
    public long EndMilliseconds { get; set; }
    public int ColorIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
}

public sealed class LSidecarSourceRecord
{
    public string FileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string AbsolutePath { get; set; } = string.Empty;
    public long Length { get; set; }
    public long LastWriteUtcTicks { get; set; }
    public long DurationMilliseconds { get; set; }
    public string PartialHash { get; set; } = string.Empty;
}

public sealed class LSidecar
{
    public const string LSidecarExtension = ".cad";

    public int Version { get; set; } = 1;

    public LSidecarSourceRecord Source { get; set; } = new();

    public List<long> KeyframeDeltas { get; set; } = new();

    public List<int> ScannedSpans { get; set; } = new();

    public int SpanGridMilliseconds { get; set; }

    public List<LSidecarSectionRecord> Sections { get; set; } = new();

    public IReadOnlyList<int> LSidecarScannedSpansRead(int lSidecarSpanGridMilliseconds) =>
        SpanGridMilliseconds == lSidecarSpanGridMilliseconds ? ScannedSpans : Array.Empty<int>();

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
            Source = new LSidecarSourceRecord
            {
                FileName = Path.GetFileName(lSidecarSourcePath),
                RelativePath = LSidecarRelativeCreate(lSidecarFolder, lSidecarSourcePath),
                AbsolutePath = lSidecarSourcePath,
                Length = lSidecarIdentity.LKeyframeSourceLength,
                LastWriteUtcTicks = lSidecarIdentity.LKeyframeSourceLastWriteUtcTicks,
                DurationMilliseconds = lSidecarIdentity.LKeyframeSourceDurationMilliseconds,
                PartialHash = lSidecarIdentity.LKeyframeSourcePartialHash
            },
            KeyframeDeltas = LSidecarKeyframeEncode(lSidecarKeyframeMilliseconds),
            ScannedSpans = lSidecarScannedSpans.Where(lSpan => lSpan >= 0).Distinct().Order().ToList(),
            SpanGridMilliseconds = lSidecarSpanGridMilliseconds,
            Sections = lSidecarSections.ToList()
        };
    }

    public static List<long> LSidecarKeyframeEncode(IReadOnlyCollection<long> lSidecarKeyframeMilliseconds)
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

    public static IReadOnlyList<long> LSidecarKeyframeDecode(IReadOnlyList<long> lSidecarDeltas)
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

    public IReadOnlyList<long> LSidecarKeyframesRead() => LSidecarKeyframeDecode(KeyframeDeltas);

    public bool LSidecarSourceMatch(LKeyframeSourceIdentity lSidecarIdentity) =>
        Source.Length == lSidecarIdentity.LKeyframeSourceLength
        && string.Equals(Source.PartialHash, lSidecarIdentity.LKeyframeSourcePartialHash, StringComparison.Ordinal);

    public string LSidecarJsonCreate() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = false });

    public static LSidecar? LSidecarParse(string lSidecarJson)
    {
        try
        {
            return JsonSerializer.Deserialize<LSidecar>(lSidecarJson);
        }
        catch (JsonException)
        {
            return null;
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
