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

    public static bool LKeyframeCacheSave(
        LKeyframeSourceIdentity identity,
        IReadOnlyCollection<long> keyframeMilliseconds,
        IReadOnlyCollection<int> scannedSpanIndexes)
    {
        string cachePath = LKeyframePathCreate(identity);
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
        string tempPath = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string? cacheFolder = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrWhiteSpace(cacheFolder))
            {
                Directory.CreateDirectory(cacheFolder);
            }

            File.WriteAllText(tempPath, json);
            File.Move(tempPath, cachePath, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
            }

            return false;
        }
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
