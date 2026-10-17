using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Media;

public static class LKeyframeCacheStore
{
    private const string LKeyframeSubFolder = "LKeyframeList";

    public static bool LKeyframeCacheLoad(
        LKeyframeSourceIdentity identity,
        int spanGridMilliseconds,
        out IReadOnlyList<long> keyframeMilliseconds,
        out IReadOnlyList<int> scannedSpanIndexes)
    {
        keyframeMilliseconds = Array.Empty<long>();
        scannedSpanIndexes = Array.Empty<int>();
        string cachePath = LKeyframePathCreate(identity);
        if (!File.Exists(cachePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(cachePath);
            LKeyframeCacheDocument? record = JsonSerializer.Deserialize<LKeyframeCacheDocument>(json);
            if (record is null || !LKeyframeCacheMatch(identity, record))
            {
                return false;
            }

            keyframeMilliseconds = (record.LKeyframeMilliseconds ?? Array.Empty<long>())
                .Where(ms => ms >= 0)
                .Distinct()
                .OrderBy(ms => ms)
                .ToArray();
            scannedSpanIndexes = (record.LKeyframeSpanGrid == spanGridMilliseconds
                    ? record.LKeyframeSpanIndexes ?? Array.Empty<int>()
                    : Array.Empty<int>())
                .Where(index => index >= 0)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool LKeyframeCacheSave(
        LKeyframeSourceIdentity identity,
        IReadOnlyCollection<long> keyframeMilliseconds,
        IReadOnlyCollection<int> scannedSpanIndexes,
        int spanGridMilliseconds)
    {
        string cachePath = LKeyframePathCreate(identity);
        var record = new LKeyframeCacheDocument
        {
            LSourcePath = identity.LKeyframeSourcePath,
            LSourceLength = identity.LKeyframeSourceLength,
            LKeyframeSourceTicks = identity.LKeyframeWriteTicks,
            LSourceDurationMilliseconds = identity.LKeyframeSourceDuration,
            LSourcePartialHash = identity.LKeyframePartialHash,
            LKeyframeMilliseconds = keyframeMilliseconds.OrderBy(ms => ms).ToArray(),
            LKeyframeSpanIndexes = scannedSpanIndexes.OrderBy(index => index).ToArray(),
            LKeyframeSpanGrid = spanGridMilliseconds
        };

        string json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        return LMediaCache.LMediaCacheSave(cachePath, json);
    }

    private static bool LKeyframeCacheMatch(LKeyframeSourceIdentity identity, LKeyframeCacheDocument record)
    {
        return string.Equals(record.LSourcePath, identity.LKeyframeSourcePath, StringComparison.OrdinalIgnoreCase)
            && record.LSourceLength == identity.LKeyframeSourceLength
            && record.LKeyframeSourceTicks == identity.LKeyframeWriteTicks
            && record.LSourceDurationMilliseconds == identity.LKeyframeSourceDuration
            && string.Equals(record.LSourcePartialHash, identity.LKeyframePartialHash, StringComparison.Ordinal);
    }

    private static string LKeyframePathCreate(LKeyframeSourceIdentity identity)
    {
        return Path.Combine(
            LMediaCache.LMediaHomeRead(),
            LKeyframeSubFolder,
            identity.LKeyframeCacheKey + ".json");
    }

    private sealed class LKeyframeCacheDocument
    {
        public string LSourcePath { get; set; } = "";

        public long LSourceLength { get; set; }

        public long LKeyframeSourceTicks { get; set; }

        public long LSourceDurationMilliseconds { get; set; }

        public string LSourcePartialHash { get; set; } = "";

        public long[]? LKeyframeMilliseconds { get; set; }

        public int[]? LKeyframeSpanIndexes { get; set; }

        public int LKeyframeSpanGrid { get; set; }
    }
}
