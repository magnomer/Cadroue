using System.Text.Json;

namespace Cadroue.Media;

public static class LKeyframeCacheStore
{
    private const string LKeyframeCacheFolder = "Cadroue";
    private const string LKeyframeSubFolder = "LKeyframeList";

    public static bool LKeyframeCacheLoad(
        LKeyframeSourceIdentity identity,
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
            LKeyframeCacheRecord? record = JsonSerializer.Deserialize<LKeyframeCacheRecord>(json);
            if (record is null || !LKeyframeCacheMatch(identity, record))
            {
                return false;
            }

            keyframeMilliseconds = (record.LKeyframeMilliseconds ?? Array.Empty<long>())
                .Where(ms => ms >= 0)
                .Distinct()
                .OrderBy(ms => ms)
                .ToArray();
            scannedSpanIndexes = (record.LKeyframeSpanIndexes ?? Array.Empty<int>())
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

    public static void LKeyframeCacheSave(
        LKeyframeSourceIdentity identity,
        IReadOnlyCollection<long> keyframeMilliseconds,
        IReadOnlyCollection<int> scannedSpanIndexes)
    {
        string cachePath = LKeyframePathCreate(identity);
        string? cacheFolder = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(cacheFolder))
        {
            Directory.CreateDirectory(cacheFolder);
        }

        var record = new LKeyframeCacheRecord
        {
            LSourcePath = identity.LKeyframeSourcePath,
            LSourceLength = identity.LKeyframeSourceLength,
            LKeyframeSourceTicks = identity.LKeyframeWriteTicks,
            LSourceDurationMilliseconds = identity.LKeyframeSourceDuration,
            LSourcePartialHash = identity.LKeyframePartialHash,
            LKeyframeMilliseconds = keyframeMilliseconds.OrderBy(ms => ms).ToArray(),
            LKeyframeSpanIndexes = scannedSpanIndexes.OrderBy(index => index).ToArray()
        };

        string json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(cachePath, json);
    }

    private static bool LKeyframeCacheMatch(LKeyframeSourceIdentity identity, LKeyframeCacheRecord record)
    {
        return string.Equals(record.LSourcePath, identity.LKeyframeSourcePath, StringComparison.OrdinalIgnoreCase)
            && record.LSourceLength == identity.LKeyframeSourceLength
            && record.LKeyframeSourceTicks == identity.LKeyframeWriteTicks
            && record.LSourceDurationMilliseconds == identity.LKeyframeSourceDuration
            && string.Equals(record.LSourcePartialHash, identity.LKeyframePartialHash, StringComparison.Ordinal);
    }

    private static string LKeyframePathCreate(LKeyframeSourceIdentity identity)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(
            appData,
            LKeyframeCacheFolder,
            LKeyframeSubFolder,
            identity.LKeyframeCacheKey + ".json");
    }

    private sealed class LKeyframeCacheRecord
    {
        public string LSourcePath { get; set; } = "";

        public long LSourceLength { get; set; }

        public long LKeyframeSourceTicks { get; set; }

        public long LSourceDurationMilliseconds { get; set; }

        public string LSourcePartialHash { get; set; } = "";

        public long[]? LKeyframeMilliseconds { get; set; }

        public int[]? LKeyframeSpanIndexes { get; set; }
    }
}
