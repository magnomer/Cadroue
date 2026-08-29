using System.IO;
using System.Reflection;
using System.Text.Json;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal sealed class LLocalizationAutopsy
{
    private const string LLocalizationAutopsyPrefix = "localization.ffmpeg-error.";
    private const string LLocalizationAutopsySuffix = ".json";
    private const string LLocalizationAutopsyDefault = "en";

    private static readonly object lLocalizationAutopsyGate = new();
    private static LLocalizationAutopsy? lLocalizationAutopsyFallback;
    private static LLocalizationAutopsy? lLocalizationAutopsyCurrent;
    private static string? lLocalizationAutopsyCode;

    private readonly IReadOnlyDictionary<string, string> lLocalizationAutopsyValues;

    private LLocalizationAutopsy(IReadOnlyDictionary<string, string> lLocalizationAutopsyValues) =>
        this.lLocalizationAutopsyValues = lLocalizationAutopsyValues;

    internal static bool LLocalizationAutopsyRead(string lLocalizationAutopsyKey, out string lLocalizationAutopsyValue)
    {
        lock (lLocalizationAutopsyGate)
        {
            string lLocalizationAutopsyLanguage = LLocalization.LLocalizationLanguageRead();
            if (!string.Equals(
                    lLocalizationAutopsyLanguage,
                    lLocalizationAutopsyCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                lLocalizationAutopsyCurrent = LLocalizationAutopsyLoad(lLocalizationAutopsyLanguage);
                lLocalizationAutopsyCode = lLocalizationAutopsyLanguage;
            }

            lLocalizationAutopsyFallback ??= LLocalizationAutopsyLoad(LLocalizationAutopsyDefault);

            if (lLocalizationAutopsyCurrent is not null
                && lLocalizationAutopsyCurrent.lLocalizationAutopsyValues.TryGetValue(
                    lLocalizationAutopsyKey, out string? lLocalizationAutopsySelected))
            {
                lLocalizationAutopsyValue = lLocalizationAutopsySelected;
                return true;
            }

            if (lLocalizationAutopsyFallback is not null
                && lLocalizationAutopsyFallback.lLocalizationAutopsyValues.TryGetValue(
                    lLocalizationAutopsyKey, out string? lLocalizationAutopsyBackup))
            {
                lLocalizationAutopsyValue = lLocalizationAutopsyBackup;
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
                LLocalizationAutopsyPrefix + lLocalizationAutopsyCode + LLocalizationAutopsySuffix);
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
