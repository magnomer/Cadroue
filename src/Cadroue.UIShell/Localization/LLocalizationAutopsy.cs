using System.IO;
using System.Reflection;
using System.Text.Json;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal sealed class LLocalizationAutopsy
{
    private const string LLocalizationAutopsyResourcePrefix = "localization.ffmpeg-error.";
    private const string LLocalizationAutopsyResourceSuffix = ".json";
    private const string LLocalizationAutopsyFallbackCode = "en";

    private static readonly object lLocalizationAutopsyGate = new();
    private static LLocalizationAutopsy? lLocalizationAutopsyFallbackMap;
    private static LLocalizationAutopsy? lLocalizationAutopsyCurrentMap;
    private static string? lLocalizationAutopsyCurrentCode;

    private readonly IReadOnlyDictionary<string, string> lLocalizationAutopsyValues;

    private LLocalizationAutopsy(IReadOnlyDictionary<string, string> lLocalizationAutopsyValues) =>
        this.lLocalizationAutopsyValues = lLocalizationAutopsyValues;

    internal static bool LLocalizationAutopsyRead(string lLocalizationAutopsyKey, out string lLocalizationAutopsyValue)
    {
        lock (lLocalizationAutopsyGate)
        {
            string lLocalizationAutopsyCode = LLocalization.LLocalizationLanguageRead();
            if (!string.Equals(
                    lLocalizationAutopsyCode,
                    lLocalizationAutopsyCurrentCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                lLocalizationAutopsyCurrentMap = LLocalizationAutopsyLoad(lLocalizationAutopsyCode);
                lLocalizationAutopsyCurrentCode = lLocalizationAutopsyCode;
            }

            lLocalizationAutopsyFallbackMap ??= LLocalizationAutopsyLoad(LLocalizationAutopsyFallbackCode);

            if (lLocalizationAutopsyCurrentMap is not null
                && lLocalizationAutopsyCurrentMap.lLocalizationAutopsyValues.TryGetValue(
                    lLocalizationAutopsyKey, out string? lLocalizationAutopsySelected))
            {
                lLocalizationAutopsyValue = lLocalizationAutopsySelected;
                return true;
            }

            if (lLocalizationAutopsyFallbackMap is not null
                && lLocalizationAutopsyFallbackMap.lLocalizationAutopsyValues.TryGetValue(
                    lLocalizationAutopsyKey, out string? lLocalizationAutopsyFallback))
            {
                lLocalizationAutopsyValue = lLocalizationAutopsyFallback;
                return true;
            }

            lLocalizationAutopsyValue = string.Empty;
            return false;
        }
    }

    private static LLocalizationAutopsy? LLocalizationAutopsyLoad(string lLocalizationAutopsyCode)
    {
        try
        {
            using Stream? lLocalizationAutopsyStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                LLocalizationAutopsyResourcePrefix + lLocalizationAutopsyCode + LLocalizationAutopsyResourceSuffix);
            if (lLocalizationAutopsyStream is null)
            {
                return null;
            }

            using var lLocalizationAutopsyReader = new StreamReader(lLocalizationAutopsyStream);
            return LLocalizationAutopsyParse(lLocalizationAutopsyReader.ReadToEnd());
        }
        catch (Exception lLocalizationAutopsyException)
        {
            LTraceLog.LTraceErrorRecord(
                $"FFmpeg-error prose could not be loaded: {lLocalizationAutopsyCode}", lLocalizationAutopsyException);
            return null;
        }
    }

    private static LLocalizationAutopsy LLocalizationAutopsyParse(string lLocalizationAutopsyJson)
    {
        using JsonDocument lLocalizationAutopsyDocument = JsonDocument.Parse(
            lLocalizationAutopsyJson,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });

        if (lLocalizationAutopsyDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("FFmpeg-error prose file must contain one JSON object.");
        }

        var lLocalizationAutopsyValues = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty lLocalizationAutopsyProperty in
            lLocalizationAutopsyDocument.RootElement.EnumerateObject())
        {
            if (lLocalizationAutopsyProperty.Value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"FFmpeg-error prose key '{lLocalizationAutopsyProperty.Name}' must have a string value.");
            }

            lLocalizationAutopsyValues[lLocalizationAutopsyProperty.Name] =
                lLocalizationAutopsyProperty.Value.GetString() ?? string.Empty;
        }

        return new LLocalizationAutopsy(lLocalizationAutopsyValues);
    }
}
