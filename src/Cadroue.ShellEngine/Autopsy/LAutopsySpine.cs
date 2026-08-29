using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Cadroue.ShellEngine;

internal readonly record struct LAutopsySpine(
    string? LAutopsySpineSymbol,
    string LAutopsySpineCategory,
    string LAutopsySpineSeverity,
    bool LAutopsySpineVisible,
    string LAutopsySpineRetryable,
    string LAutopsySpineClass)
{
    private const string LAutopsySpineResource = "autopsy.ffmpeg-error-spine.json";
    private const string LAutopsySpineUnknown = "unknown_negative";
    private const string LAutopsySpineUnexpected = "unexpected_positive";

    private static readonly object lAutopsySpineGate = new();
    private static IReadOnlyDictionary<string, LAutopsySpine>? lAutopsySpineErrors;
    private static LAutopsySpine lAutopsySpineNegative;
    private static LAutopsySpine lAutopsySpinePositive;

    internal static bool LAutopsySpineRead(string lAutopsySpineCode, out LAutopsySpine lAutopsySpineEntry)
    {
        LAutopsySpineLoad();
        return lAutopsySpineErrors!.TryGetValue(lAutopsySpineCode, out lAutopsySpineEntry);
    }

    internal static LAutopsySpine LAutopsySpineResolve(bool lAutopsySpineSign)
    {
        LAutopsySpineLoad();
        return lAutopsySpineSign ? lAutopsySpineNegative : lAutopsySpinePositive;
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
                .GetManifestResourceStream(LAutopsySpineResource);
            if (lAutopsySpineStream is null)
            {
                throw new InvalidDataException($"FFmpeg-error spine resource missing: {LAutopsySpineResource}.");
            }

            using JsonDocument lAutopsySpineDocument = JsonDocument.Parse(lAutopsySpineStream);
            JsonElement lAutopsySpineRoot = lAutopsySpineDocument.RootElement;

            var lAutopsySpineErrorMap = new Dictionary<string, LAutopsySpine>(StringComparer.Ordinal);
            foreach (JsonProperty lAutopsySpineProperty in lAutopsySpineRoot.GetProperty("errors").EnumerateObject())
            {
                lAutopsySpineErrorMap[lAutopsySpineProperty.Name] = LAutopsySpineParse(lAutopsySpineProperty.Value);
            }

            JsonElement lAutopsySpineFallbacks = lAutopsySpineRoot.GetProperty("fallbacks");
            lAutopsySpineNegative = LAutopsySpineParse(
                lAutopsySpineFallbacks.GetProperty(LAutopsySpineUnknown));
            lAutopsySpinePositive = LAutopsySpineParse(
                lAutopsySpineFallbacks.GetProperty(LAutopsySpineUnexpected));

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
