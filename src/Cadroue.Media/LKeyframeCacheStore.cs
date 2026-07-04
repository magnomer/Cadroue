using System.Text.Json;

namespace Cadroue.Media;

public static class LKeyframeCacheStore
{
    private const string LKeyframeCacheFolderName = "Cadroue";
    private const string LKeyframeCacheSubFolderName = "LKeyframes";

    public static bool LKeyframeCacheLoad(
        LKeyframeSourceIdentity identity,
        out IReadOnlyList<long> keyframeMilliseconds,
        out IReadOnlyList<int> scannedSpanIndexes)
    {
        keyframeMilliseconds = Array.Empty<long>();
        scannedSpanIndexes = Array.Empty<int>();
        string cachePath = LKeyframeCachePathCreate(identity);
        if (!File.Exists(cachePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(cachePath);
            LKeyframeCacheRecord? record = JsonSerializer.Deserialize<LKeyframeCacheRecord>(json);
            if (record is null || !LKeyframeCacheIdentityMatch(identity, record))
            {
                return false;
            }

            keyframeMilliseconds = (record.LKeyframeMilliseconds ?? Array.Empty<long>())
                .Where(ms => ms >= 0)
                .Distinct()
                .OrderBy(ms => ms)
                .ToArray();
            scannedSpanIndexes = (record.LScannedSpanIndexes ?? Array.Empty<int>())
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
        string cachePath = LKeyframeCachePathCreate(identity);
        string? cacheFolder = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(cacheFolder))
        {
            Directory.CreateDirectory(cacheFolder);
        }

        var record = new LKeyframeCacheRecord
        {
            LSourcePath = identity.LKeyframeSourcePath,
            LSourceLength = identity.LKeyframeSourceLength,
            LSourceLastWriteUtcTicks = identity.LKeyframeSourceLastWriteUtcTicks,
            LSourceDurationMilliseconds = identity.LKeyframeSourceDurationMilliseconds,
            LSourcePartialHash = identity.LKeyframeSourcePartialHash,
            LKeyframeMilliseconds = keyframeMilliseconds.OrderBy(ms => ms).ToArray(),
            LScannedSpanIndexes = scannedSpanIndexes.OrderBy(index => index).ToArray()
        };

        string json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(cachePath, json);
    }

    private static bool LKeyframeCacheIdentityMatch(LKeyframeSourceIdentity identity, LKeyframeCacheRecord record)
    {
        return string.Equals(record.LSourcePath, identity.LKeyframeSourcePath, StringComparison.OrdinalIgnoreCase)
            && record.LSourceLength == identity.LKeyframeSourceLength
            && record.LSourceLastWriteUtcTicks == identity.LKeyframeSourceLastWriteUtcTicks
            && record.LSourceDurationMilliseconds == identity.LKeyframeSourceDurationMilliseconds
            && string.Equals(record.LSourcePartialHash, identity.LKeyframeSourcePartialHash, StringComparison.Ordinal);
    }

    private static string LKeyframeCachePathCreate(LKeyframeSourceIdentity identity)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(
            appData,
            LKeyframeCacheFolderName,
            LKeyframeCacheSubFolderName,
            identity.LKeyframeSourceCacheKey + ".json");
    }

    private sealed class LKeyframeCacheRecord
    {
        public string LSourcePath { get; set; } = "";

        public long LSourceLength { get; set; }

        public long LSourceLastWriteUtcTicks { get; set; }

        public long LSourceDurationMilliseconds { get; set; }

        public string LSourcePartialHash { get; set; } = "";

        public long[]? LKeyframeMilliseconds { get; set; }

        public int[]? LScannedSpanIndexes { get; set; }
    }
}
