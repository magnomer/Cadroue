using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cadroue.UIShell;

internal sealed partial class LLocalizationCatalog
{
    private static readonly Regex lLocalizationKeyPattern = LLocalizationKeyCreate();
    private static readonly Regex lLocalizationTermPattern = LLocalizationTermCreate();
    private static readonly Regex lLocalizationMeaninglessPattern = LLocalizationMeaninglessCreate();

    private readonly IReadOnlyDictionary<string, string> lLocalizationCatalogValues;

    private LLocalizationCatalog(
        string lLocalizationCatalogCode,
        IReadOnlyDictionary<string, string> lLocalizationCatalogValues)
    {
        LLocalizationCatalogCode = lLocalizationCatalogCode;
        this.lLocalizationCatalogValues = lLocalizationCatalogValues;
    }

    internal string LLocalizationCatalogCode { get; }

    internal string LLocalizationCatalogName =>
        LLocalizationCatalogRead("Localization.Language.Name") ?? LLocalizationCatalogCode;

    internal static LLocalizationCatalog LLocalizationCatalogLoad(string lLocalizationCode, string lLocalizationJson)
    {
        string lLocalizationFilePath = $"{lLocalizationCode}.json";
        using JsonDocument lLocalizationDocument = JsonDocument.Parse(
            lLocalizationJson,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });

        if (lLocalizationDocument.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Localization file '{lLocalizationFilePath}' must contain one JSON object.");
        }

        var lLocalizationRawValues = new Dictionary<string, string>(StringComparer.Ordinal);
        bool lLocalizationFirst = true;
        bool lLocalizationTermSection = true;
        foreach (JsonProperty lLocalizationProperty in lLocalizationDocument.RootElement.EnumerateObject())
        {
            string lLocalizationKey = lLocalizationProperty.Name;
            LLocalizationKeyValidate(lLocalizationKey, lLocalizationFilePath);
            bool lLocalizationTerm = lLocalizationKey.StartsWith("Terms.", StringComparison.Ordinal);
            if (lLocalizationFirst && !lLocalizationTerm)
            {
                throw new InvalidDataException(
                    $"Localization file '{lLocalizationFilePath}' must start with Terms.* entries.");
            }

            lLocalizationFirst = false;
            if (!lLocalizationTerm)
            {
                lLocalizationTermSection = false;
            }
            else if (!lLocalizationTermSection)
            {
                throw new InvalidDataException(
                    $"Localization file '{lLocalizationFilePath}' must keep every Terms.* entry at the beginning.");
            }

            if (lLocalizationProperty.Value.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"Localization key '{lLocalizationKey}' in '{lLocalizationFilePath}' must have a string value.");
            }

            if (!lLocalizationRawValues.TryAdd(lLocalizationKey, lLocalizationProperty.Value.GetString() ?? string.Empty))
            {
                throw new InvalidDataException(
                    $"Localization key '{lLocalizationKey}' is duplicated in '{lLocalizationFilePath}'.");
            }
        }

        if (lLocalizationRawValues.Count == 0)
        {
            throw new InvalidDataException($"Localization file '{lLocalizationFilePath}' is empty.");
        }

        var lLocalizationResolvedValues = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string lLocalizationKey in lLocalizationRawValues.Keys)
        {
            lLocalizationResolvedValues[lLocalizationKey] = LLocalizationTermResolve(
                lLocalizationKey,
                lLocalizationRawValues,
                lLocalizationResolvedValues,
                new HashSet<string>(StringComparer.Ordinal),
                lLocalizationFilePath);
        }

        return new LLocalizationCatalog(lLocalizationCode, lLocalizationResolvedValues);
    }

    internal string? LLocalizationCatalogRead(string lLocalizationKey) =>
        lLocalizationCatalogValues.TryGetValue(lLocalizationKey, out string? lLocalizationText)
            ? lLocalizationText
            : null;

    private static string LLocalizationTermResolve(
        string lLocalizationKey,
        IReadOnlyDictionary<string, string> lLocalizationRawValues,
        IDictionary<string, string> lLocalizationResolvedValues,
        ISet<string> lLocalizationPath,
        string lLocalizationFilePath)
    {
        if (lLocalizationResolvedValues.TryGetValue(lLocalizationKey, out string? lLocalizationResolved))
        {
            return lLocalizationResolved;
        }

        if (!lLocalizationRawValues.TryGetValue(lLocalizationKey, out string? lLocalizationRaw))
        {
            throw new InvalidDataException(
                $"Localization term '{lLocalizationKey}' is missing from '{lLocalizationFilePath}'.");
        }

        if (!lLocalizationPath.Add(lLocalizationKey))
        {
            throw new InvalidDataException(
                $"Localization term reference cycle includes '{lLocalizationKey}' in '{lLocalizationFilePath}'.");
        }

        string lLocalizationResult = lLocalizationTermPattern.Replace(
            lLocalizationRaw,
            lLocalizationMatch =>
            {
                string lLocalizationTermKey = lLocalizationMatch.Groups[1].Value;
                return LLocalizationTermResolve(
                    lLocalizationTermKey,
                    lLocalizationRawValues,
                    lLocalizationResolvedValues,
                    lLocalizationPath,
                    lLocalizationFilePath);
            });

        lLocalizationPath.Remove(lLocalizationKey);
        lLocalizationResolvedValues[lLocalizationKey] = lLocalizationResult;
        return lLocalizationResult;
    }

    private static void LLocalizationKeyValidate(string lLocalizationKey, string lLocalizationFilePath)
    {
        if (!lLocalizationKeyPattern.IsMatch(lLocalizationKey)
            || lLocalizationKey.Split('.').Any(lLocalizationSegment => lLocalizationSegment.Length < 2)
            || lLocalizationMeaninglessPattern.IsMatch(lLocalizationKey))
        {
            throw new InvalidDataException(
                $"Localization key '{lLocalizationKey}' in '{lLocalizationFilePath}' is not semantically meaningful.");
        }
    }

    [GeneratedRegex(@"^(?:Terms|[A-Z][A-Za-z0-9]*)(?:\.[A-Z][A-Za-z0-9]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex LLocalizationKeyCreate();

    [GeneratedRegex(@"\{(Terms\.[A-Z][A-Za-z0-9]*(?:\.[A-Z][A-Za-z0-9]*)*)\}", RegexOptions.CultureInvariant)]
    private static partial Regex LLocalizationTermCreate();

    [GeneratedRegex(@"(?:^|\.)(?:Key|Text|Value|Label|Item|Thing)\d+(?:\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LLocalizationMeaninglessCreate();
}
