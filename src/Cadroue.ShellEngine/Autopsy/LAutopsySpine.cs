using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Cadroue.ShellEngine;

internal readonly record struct LAutopsySpine(
    string? LAutopsySpineSymbol,
    string LAutopsySpineCategory,
    string LAutopsySpineSeverity,
    bool LAutopsySpineUserVisible,
    string LAutopsySpineRetryable,
    string LAutopsySpineCodeClass)
{
    private const string LAutopsySpineResourceName = "autopsy.ffmpeg-error-spine.json";
    private const string LAutopsySpineFallbackNegativeKey = "unknown_negative";
    private const string LAutopsySpineFallbackPositiveKey = "unexpected_positive";

    private static readonly object lAutopsySpineGate = new();
    private static IReadOnlyDictionary<string, LAutopsySpine>? lAutopsySpineErrors;
    private static LAutopsySpine lAutopsySpineFallbackNegative;
    private static LAutopsySpine lAutopsySpineFallbackPositive;

    internal static bool LAutopsySpineRead(string lAutopsySpineCode, out LAutopsySpine lAutopsySpineEntry)
    {
        LAutopsySpineLoad();
        return lAutopsySpineErrors!.TryGetValue(lAutopsySpineCode, out lAutopsySpineEntry);
    }

    internal static LAutopsySpine LAutopsySpineFallbackRead(bool lAutopsySpineNegative)
    {
        LAutopsySpineLoad();
        return lAutopsySpineNegative ? lAutopsySpineFallbackNegative : lAutopsySpineFallbackPositive;
    }

    private static void LAutopsySpineLoad()
    {
        if (lAutopsySpineErrors is not null)
        {
            return;
        }

        lock (lAutopsySpineGate)
        {
            if (lAutopsySpineErrors is not null)
            {
                return;
            }

            using Stream? lAutopsySpineStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(LAutopsySpineResourceName);
            if (lAutopsySpineStream is null)
            {
                throw new InvalidDataException($"FFmpeg-error spine resource missing: {LAutopsySpineResourceName}.");
            }

            using JsonDocument lAutopsySpineDocument = JsonDocument.Parse(lAutopsySpineStream);
            JsonElement lAutopsySpineRoot = lAutopsySpineDocument.RootElement;

            var lAutopsySpineErrorMap = new Dictionary<string, LAutopsySpine>(StringComparer.Ordinal);
            foreach (JsonProperty lAutopsySpineProperty in lAutopsySpineRoot.GetProperty("errors").EnumerateObject())
            {
                lAutopsySpineErrorMap[lAutopsySpineProperty.Name] = LAutopsySpineParse(lAutopsySpineProperty.Value);
            }

            JsonElement lAutopsySpineFallbacks = lAutopsySpineRoot.GetProperty("fallbacks");
            lAutopsySpineFallbackNegative = LAutopsySpineParse(
                lAutopsySpineFallbacks.GetProperty(LAutopsySpineFallbackNegativeKey));
            lAutopsySpineFallbackPositive = LAutopsySpineParse(
                lAutopsySpineFallbacks.GetProperty(LAutopsySpineFallbackPositiveKey));

            lAutopsySpineErrors = lAutopsySpineErrorMap;
        }
    }

    private static LAutopsySpine LAutopsySpineParse(JsonElement lAutopsySpineElement)
    {
        JsonElement lAutopsySpineSymbolElement = lAutopsySpineElement.GetProperty("symbol");
        return new LAutopsySpine(
            lAutopsySpineSymbolElement.ValueKind == JsonValueKind.String
                ? lAutopsySpineSymbolElement.GetString()
                : null,
            lAutopsySpineElement.GetProperty("category").GetString() ?? string.Empty,
            lAutopsySpineElement.GetProperty("severity").GetString() ?? string.Empty,
            lAutopsySpineElement.GetProperty("user_visible").GetBoolean(),
            lAutopsySpineElement.GetProperty("retryable").GetString() ?? string.Empty,
            lAutopsySpineElement.GetProperty("code_class").GetString() ?? string.Empty);
    }
}
