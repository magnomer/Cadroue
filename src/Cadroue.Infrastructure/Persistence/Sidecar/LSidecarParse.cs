using System.Text.Json;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

internal static class LSidecarParse
{
    private static readonly JsonSerializerOptions lSidecarJsonOptions = new() { WriteIndented = false };

    internal static string LSidecarCoreFormat(LSidecarCoreRecord lSidecarCore) =>
        JsonSerializer.Serialize(lSidecarCore, lSidecarJsonOptions);

    internal static string LSidecarCacheFormat(LSidecarCacheRecord lSidecarCache) =>
        JsonSerializer.Serialize(lSidecarCache, lSidecarJsonOptions);

    internal static LSidecarCoreRecord? LSidecarCoreParse(string lSidecarJson)
    {
        JsonDocument lSidecarDocument;
        try
        {
            lSidecarDocument = JsonDocument.Parse(lSidecarJson);
        }
        catch (JsonException)
        {
            return null;
        }

        using (lSidecarDocument)
        {
            JsonElement lSidecarRoot = lSidecarDocument.RootElement;
            var lSidecarCore = new LSidecarCoreRecord
            {
                LSidecarVersion = LSidecarIntRead(lSidecarRoot, "LSidecarVersion", 2),
                LSidecarSource = LSidecarMemberRead<LSidecarSourceRecord>(lSidecarRoot, "LSidecarSource") ?? new(),
                LSidecarSections = LSidecarMemberRead<List<LSidecarSectionRecord>>(lSidecarRoot, "LSidecarSections") ?? new(),
                LSidecarEdit = LSidecarMemberRead<LSidecarEditRecord>(lSidecarRoot, "LSidecarEdit"),
                LSidecarAudio = LSidecarMemberRead<LSidecarAudioRecord>(lSidecarRoot, "LSidecarAudio"),
                LSidecarLoudness = LSidecarDoubleRead(lSidecarRoot, "LSidecarLoudness")
            };
            LSidecarCoreNormalize(lSidecarCore);
            return lSidecarCore;
        }
    }

    internal static LSidecarCacheRecord? LSidecarCacheParse(string lSidecarJson)
    {
        JsonDocument lSidecarDocument;
        try
        {
            lSidecarDocument = JsonDocument.Parse(lSidecarJson);
        }
        catch (JsonException)
        {
            return null;
        }

        using (lSidecarDocument)
        {
            JsonElement lSidecarRoot = lSidecarDocument.RootElement;
            var lSidecarCache = new LSidecarCacheRecord
            {
                LSidecarVersion = LSidecarIntRead(lSidecarRoot, "LSidecarVersion", 2),
                LSidecarLength = LSidecarLongRead(lSidecarRoot, "LSidecarLength"),
                LSidecarPartialHash = LSidecarStringRead(lSidecarRoot, "LSidecarPartialHash"),
                LSidecarSpanGrid = LSidecarIntRead(lSidecarRoot, "LSidecarSpanGrid", 0),
                LSidecarKeyframeCount = LSidecarIntRead(lSidecarRoot, "LSidecarKeyframeCount", 0),
                LSidecarKeyframeLast = LSidecarLongRead(lSidecarRoot, "LSidecarKeyframeLast"),
                LSidecarScannedSpans = LSidecarMemberRead<List<int>>(lSidecarRoot, "LSidecarScannedSpans") ?? new(),
                LSidecarKeyframeDeltas = LSidecarMemberRead<List<long>>(lSidecarRoot, "LSidecarKeyframeDeltas") ?? new(),
                LSidecarWaveform = LSidecarMemberRead<LSidecarWaveformRecord>(lSidecarRoot, "LSidecarWaveform")
            };
            LSidecarCacheNormalize(lSidecarCache);
            return lSidecarCache;
        }
    }

    internal static LSidecar LSidecarCreate(LSidecarCoreRecord lSidecarCore, LSidecarCacheRecord? lSidecarCache)
    {
        LSidecarCacheRecord? lSidecarValid = LSidecarCacheValidate(lSidecarCache);
        return new LSidecar
        {
            LSidecarVersion = lSidecarCore.LSidecarVersion,
            LSidecarSource = lSidecarCore.LSidecarSource,
            LSidecarSections = lSidecarCore.LSidecarSections,
            LSidecarEdit = lSidecarCore.LSidecarEdit,
            LSidecarAudio = lSidecarCore.LSidecarAudio,
            LSidecarLoudness = lSidecarCore.LSidecarLoudness,
            LSidecarKeyframeDeltas = lSidecarValid?.LSidecarKeyframeDeltas ?? new(),
            LSidecarScannedSpans = lSidecarValid?.LSidecarScannedSpans ?? new(),
            LSidecarSpanGrid = lSidecarValid?.LSidecarSpanGrid ?? 0,
            LSidecarWaveform = lSidecarValid?.LSidecarWaveform
        };
    }

    internal static LSidecarCacheRecord? LSidecarCacheValidate(LSidecarCacheRecord? lSidecarCache)
    {
        if (lSidecarCache is not { LSidecarKeyframeCount: > 0 })
        {
            return lSidecarCache;
        }

        IReadOnlyList<long> lSidecarKeyframes = LSidecarKeyframe.LSidecarKeyframeParse(lSidecarCache.LSidecarKeyframeDeltas);
        if (LSidecarKeyframe.LSidecarKeyframeCheck(
                lSidecarKeyframes,
                lSidecarCache.LSidecarKeyframeCount,
                lSidecarCache.LSidecarKeyframeLast))
        {
            return lSidecarCache;
        }

        lSidecarCache.LSidecarKeyframeDeltas = new();
        lSidecarCache.LSidecarScannedSpans = new();
        lSidecarCache.LSidecarSpanGrid = 0;
        lSidecarCache.LSidecarKeyframeCount = 0;
        lSidecarCache.LSidecarKeyframeLast = 0;
        return lSidecarCache;
    }

    private static T? LSidecarMemberRead<T>(JsonElement lSidecarRoot, string lSidecarName)
    {
        if (!lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
            || lSidecarElement.ValueKind == JsonValueKind.Null)
        {
            return default;
        }

        try
        {
            return lSidecarElement.Deserialize<T>(lSidecarJsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static int LSidecarIntRead(JsonElement lSidecarRoot, string lSidecarName, int lSidecarFallback) =>
        lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
        && lSidecarElement.ValueKind == JsonValueKind.Number
        && lSidecarElement.TryGetInt32(out int lSidecarValue)
            ? lSidecarValue
            : lSidecarFallback;

    private static long LSidecarLongRead(JsonElement lSidecarRoot, string lSidecarName) =>
        lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
        && lSidecarElement.ValueKind == JsonValueKind.Number
        && lSidecarElement.TryGetInt64(out long lSidecarValue)
            ? lSidecarValue
            : 0;

    private static double LSidecarDoubleRead(JsonElement lSidecarRoot, string lSidecarName) =>
        lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
        && lSidecarElement.ValueKind == JsonValueKind.Number
        && lSidecarElement.TryGetDouble(out double lSidecarValue)
            ? lSidecarValue
            : 0;

    private static string LSidecarStringRead(JsonElement lSidecarRoot, string lSidecarName) =>
        lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
        && lSidecarElement.ValueKind == JsonValueKind.String
            ? lSidecarElement.GetString() ?? string.Empty
            : string.Empty;

    private static void LSidecarCoreNormalize(LSidecarCoreRecord lSidecarCore)
    {
        lSidecarCore.LSidecarSource ??= new();
        lSidecarCore.LSidecarSource.LSidecarFileName ??= string.Empty;
        lSidecarCore.LSidecarSource.LSidecarRelativePath ??= string.Empty;
        lSidecarCore.LSidecarSource.LSidecarAbsolutePath ??= string.Empty;
        lSidecarCore.LSidecarSource.LSidecarPartialHash ??= string.Empty;
        lSidecarCore.LSidecarSections ??= new();
        if (lSidecarCore.LSidecarSections.Count > LPiece.LPieceCeiling)
        {
            lSidecarCore.LSidecarSections = lSidecarCore.LSidecarSections.GetRange(0, LPiece.LPieceCeiling);
        }

        if (lSidecarCore.LSidecarEdit is { } lSidecarEdit)
        {
            lSidecarEdit.LSidecarSteps ??= new();
        }

        if (lSidecarCore.LSidecarAudio is { } lSidecarAudio)
        {
            lSidecarAudio.LSidecarSteps ??= new();
        }
    }

    private static void LSidecarCacheNormalize(LSidecarCacheRecord lSidecarCache)
    {
        lSidecarCache.LSidecarPartialHash ??= string.Empty;
        lSidecarCache.LSidecarKeyframeDeltas ??= new();
        lSidecarCache.LSidecarScannedSpans ??= new();

        if (lSidecarCache.LSidecarWaveform is { } lSidecarWaveform)
        {
            lSidecarWaveform.LSidecarPeaks ??= string.Empty;
            lSidecarWaveform.LSidecarRms ??= string.Empty;
        }
    }
}
