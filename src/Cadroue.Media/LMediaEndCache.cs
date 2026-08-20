using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaEndCache
{
    private const string LMediaEndFolder = "Cadroue";
    private const string LMediaEndStore = "LMediaEnd";
    private const long LMediaEndAbsent = -1;

    public static bool LMediaEndLoad(LKeyframeSourceIdentity lMediaIdentity, out TimeSpan? lMediaVideoEnd)
    {
        lMediaVideoEnd = null;
        string lMediaCachePath = LMediaEndResolve(lMediaIdentity);
        if (!File.Exists(lMediaCachePath))
        {
            return false;
        }

        try
        {
            string lMediaJson = File.ReadAllText(lMediaCachePath);
            LMediaEndRecord? lMediaRecord = JsonSerializer.Deserialize<LMediaEndRecord>(lMediaJson);
            if (lMediaRecord is null || !LMediaEndMatch(lMediaIdentity, lMediaRecord))
            {
                return false;
            }

            lMediaVideoEnd = lMediaRecord.LMediaEndMilliseconds < 0
                ? null
                : TimeSpan.FromMilliseconds(lMediaRecord.LMediaEndMilliseconds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool LMediaEndSave(LKeyframeSourceIdentity lMediaIdentity, TimeSpan? lMediaVideoEnd)
    {
        string lMediaCachePath = LMediaEndResolve(lMediaIdentity);
        var lMediaRecord = new LMediaEndRecord
        {
            LSourcePath = lMediaIdentity.LKeyframeSourcePath,
            LSourceLength = lMediaIdentity.LKeyframeSourceLength,
            LSourceTicks = lMediaIdentity.LKeyframeWriteTicks,
            LSourceDuration = lMediaIdentity.LKeyframeSourceDuration,
            LSourcePartialHash = lMediaIdentity.LKeyframePartialHash,
            LMediaEndMilliseconds = lMediaVideoEnd is { } lMediaEnd
                ? (long)Math.Round(lMediaEnd.TotalMilliseconds)
                : LMediaEndAbsent
        };

        string lMediaJson = JsonSerializer.Serialize(lMediaRecord, new JsonSerializerOptions { WriteIndented = true });
        string lMediaTempPath = lMediaCachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string? lMediaFolder = Path.GetDirectoryName(lMediaCachePath);
            if (!string.IsNullOrWhiteSpace(lMediaFolder))
            {
                Directory.CreateDirectory(lMediaFolder);
            }

            File.WriteAllText(lMediaTempPath, lMediaJson);
            File.Move(lMediaTempPath, lMediaCachePath, overwrite: true);
            return true;
        }
        catch (Exception lMediaException) when (lMediaException is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(lMediaTempPath))
                {
                    File.Delete(lMediaTempPath);
                }
            }
            catch (Exception lMediaCleanup) when (lMediaCleanup is IOException or UnauthorizedAccessException)
            {
            }

            return false;
        }
    }

    private static bool LMediaEndMatch(LKeyframeSourceIdentity lMediaIdentity, LMediaEndRecord lMediaRecord) =>
        string.Equals(lMediaRecord.LSourcePath, lMediaIdentity.LKeyframeSourcePath, StringComparison.OrdinalIgnoreCase)
        && lMediaRecord.LSourceLength == lMediaIdentity.LKeyframeSourceLength
        && lMediaRecord.LSourceTicks == lMediaIdentity.LKeyframeWriteTicks
        && lMediaRecord.LSourceDuration == lMediaIdentity.LKeyframeSourceDuration
        && string.Equals(lMediaRecord.LSourcePartialHash, lMediaIdentity.LKeyframePartialHash, StringComparison.Ordinal);

    private static string LMediaEndResolve(LKeyframeSourceIdentity lMediaIdentity)
    {
        string lMediaAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(
            lMediaAppData,
            LMediaEndFolder,
            LMediaEndStore,
            lMediaIdentity.LKeyframeCacheKey + ".json");
    }

    private sealed class LMediaEndRecord
    {
        public string LSourcePath { get; set; } = "";

        public long LSourceLength { get; set; }

        public long LSourceTicks { get; set; }

        public long LSourceDuration { get; set; }

        public string LSourcePartialHash { get; set; } = "";

        public long LMediaEndMilliseconds { get; set; }
    }
}
