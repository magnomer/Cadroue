using System.Text.Json;
using System.Text.Json.Nodes;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LSidecarSerializer
{
    private static readonly JsonSerializerOptions lSidecarJsonOptions = new() { WriteIndented = false };

    internal static string LSidecarCoreFormat(LSidecarCoreDocument lSidecarCore, string? lSidecarExistingJson)
    {
        JsonObject lSidecarFresh = JsonSerializer.SerializeToNode(lSidecarCore, lSidecarJsonOptions)!.AsObject();
        if (lSidecarExistingJson is null)
        {
            return lSidecarFresh.ToJsonString(lSidecarJsonOptions);
        }

        JsonObject? lSidecarExisting;
        try
        {
            lSidecarExisting = JsonNode.Parse(lSidecarExistingJson) as JsonObject;
        }
        catch (JsonException)
        {
            lSidecarExisting = null;
        }

        if (lSidecarExisting is not null)
        {
            foreach ((string lSidecarName, JsonNode? lSidecarNode) in lSidecarExisting)
            {
                if (!lSidecarFresh.ContainsKey(lSidecarName))
                {
                    lSidecarFresh[lSidecarName] = lSidecarNode?.DeepClone();
                }
            }
        }

        return lSidecarFresh.ToJsonString(lSidecarJsonOptions);
    }

    internal static string LSidecarCacheFormat(LSidecarCacheDocument lSidecarCache) =>
        JsonSerializer.Serialize(lSidecarCache, lSidecarJsonOptions);

    internal static LSidecarCoreDocument? LSidecarCoreParse(string lSidecarJson)
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
            var lSidecarCore = new LSidecarCoreDocument
            {
                LSidecarVersion = LSidecarIntRead(lSidecarRoot, "LSidecarVersion", 2),
                LSidecarSource = LSidecarMemberRead<LSidecarSourceEntry>(lSidecarRoot, "LSidecarSource") ?? new(),
                LSidecarSections = LSidecarMemberRead<List<LSidecarSectionEntry>>(
                    lSidecarRoot,
                    "LSidecarSections") ?? new(),
                LSidecarEdit = LSidecarMemberRead<LSidecarEditEntry>(lSidecarRoot, "LSidecarEdit"),
                LSidecarAudio = LSidecarMemberRead<LSidecarAudioEntry>(lSidecarRoot, "LSidecarAudio"),
                LSidecarSplit = LSidecarMemberRead<LSidecarSplitEntry>(lSidecarRoot, "LSidecarSplit"),
                LSidecarFix = LSidecarMemberRead<LSidecarFixEntry>(lSidecarRoot, "LSidecarFix")
            };
            LSidecarCoreNormalize(lSidecarCore);
            return lSidecarCore;
        }
    }

    internal static LSidecarCacheDocument? LSidecarCacheParse(string lSidecarJson)
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
            var lSidecarCache = new LSidecarCacheDocument
            {
                LSidecarVersion = LSidecarIntRead(lSidecarRoot, "LSidecarVersion", 2),
                LSidecarLength = LSidecarLongRead(lSidecarRoot, "LSidecarLength"),
                LSidecarPartialHash = LSidecarStringRead(lSidecarRoot, "LSidecarPartialHash"),
                LSidecarSpanGrid = LSidecarIntRead(lSidecarRoot, "LSidecarSpanGrid", 0),
                LSidecarKeyframeCount = LSidecarIntRead(lSidecarRoot, "LSidecarKeyframeCount", 0),
                LSidecarKeyframeLast = LSidecarLongRead(lSidecarRoot, "LSidecarKeyframeLast"),
                LSidecarScannedSpans = LSidecarMemberRead<List<int>>(lSidecarRoot, "LSidecarScannedSpans") ?? new(),
                LSidecarKeyframeDeltas = LSidecarMemberRead<List<long>>(
                    lSidecarRoot,
                    "LSidecarKeyframeDeltas") ?? new(),
                LSidecarWaveform = LSidecarMemberRead<LSidecarWaveformEntry>(lSidecarRoot, "LSidecarWaveform"),
                LSidecarDiagnosis = LSidecarMemberRead<LSidecarDiagnosisEntry>(lSidecarRoot, "LSidecarDiagnosis")
            };
            LSidecarCacheNormalize(lSidecarCache);
            return lSidecarCache;
        }
    }

    private static LSidecarMember? LSidecarMemberRead<LSidecarMember>(JsonElement lSidecarRoot, string lSidecarName)
    {
        if (!lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
            || lSidecarElement.ValueKind == JsonValueKind.Null)
        {
            return default;
        }

        try
        {
            return lSidecarElement.Deserialize<LSidecarMember>(lSidecarJsonOptions);
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

    private static string LSidecarStringRead(JsonElement lSidecarRoot, string lSidecarName) =>
        lSidecarRoot.TryGetProperty(lSidecarName, out JsonElement lSidecarElement)
        && lSidecarElement.ValueKind == JsonValueKind.String
            ? lSidecarElement.GetString() ?? string.Empty
            : string.Empty;

    private static void LSidecarCoreNormalize(LSidecarCoreDocument lSidecarCore)
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

        if (lSidecarCore.LSidecarSplit is { } lSidecarSplit)
        {
            lSidecarSplit.LSidecarSplitDetectors ??= new();
        }

        if (lSidecarCore.LSidecarFix is { } lSidecarFix)
        {
            lSidecarFix.LSidecarSteps ??= new();
        }
    }

    private static void LSidecarCacheNormalize(LSidecarCacheDocument lSidecarCache)
    {
        lSidecarCache.LSidecarPartialHash ??= string.Empty;
        lSidecarCache.LSidecarKeyframeDeltas ??= new();
        lSidecarCache.LSidecarScannedSpans ??= new();

        if (lSidecarCache.LSidecarDiagnosis is { } lSidecarDiagnosis)
        {
            lSidecarDiagnosis.LSidecarPartialHash ??= string.Empty;
            lSidecarDiagnosis.LSidecarDossiers ??= new();
        }

        if (lSidecarCache.LSidecarWaveform is { } lSidecarWaveform)
        {
            lSidecarWaveform.LSidecarPeaks ??= string.Empty;
        }
    }
}
