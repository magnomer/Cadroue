using System.IO;
using System.Globalization;
using System.Reflection;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal static class LLocalization
{
    private const string LLocalizationResourcePrefix = "localization.";
    private const string LLocalizationResourceSuffix = ".json";
    private const string LLocalizationFallbackCode = "en";

    private static readonly object lLocalizationGate = new();
    private static LLocalizationCatalog? lLocalizationFallbackCatalog;
    private static LLocalizationCatalog? lLocalizationCurrentCatalog;
    private static IReadOnlyDictionary<string, string>? lLocalizationLanguages;

    internal static void LLocalizationLoad(string? lLocalizationLanguage)
    {
        lock (lLocalizationGate)
        {
            lLocalizationFallbackCatalog = LLocalizationCatalogRead(LLocalizationFallbackCode);

            if (lLocalizationFallbackCatalog is null)
            {
                LTraceLog.LTraceErrorRecord(
                    "English localization could not be loaded from the program resources; keys will show untranslated.");
            }

            string lLocalizationCode = LLocalizationLanguageNormalize(lLocalizationLanguage);
            if (string.Equals(lLocalizationCode, LLocalizationFallbackCode, StringComparison.OrdinalIgnoreCase))
            {
                lLocalizationCurrentCatalog = lLocalizationFallbackCatalog;
            }
            else
            {
                LLocalizationCatalog? lLocalizationSelected = LLocalizationCatalogRead(lLocalizationCode);
                if (lLocalizationSelected is null)
                {
                    LTraceLog.LTraceErrorRecord(
                        $"Localization '{lLocalizationCode}' is unavailable; using {LLocalizationFallbackCode}.");
                }

                lLocalizationCurrentCatalog = lLocalizationSelected ?? lLocalizationFallbackCatalog;
            }

            lLocalizationLanguages = null;
            CultureInfo lLocalizationCulture;
            try
            {
                lLocalizationCulture = CultureInfo.GetCultureInfo(
                    lLocalizationCurrentCatalog?.LLocalizationCatalogCode ?? LLocalizationFallbackCode);
            }
            catch (CultureNotFoundException)
            {
                lLocalizationCulture = CultureInfo.GetCultureInfo(LLocalizationFallbackCode);
            }

            CultureInfo.CurrentUICulture = lLocalizationCulture;
            CultureInfo.DefaultThreadCurrentUICulture = lLocalizationCulture;
        }
    }

    internal static string LLocalizationTextRead(string lLocalizationKey)
    {
        lock (lLocalizationGate)
        {
            return lLocalizationCurrentCatalog?.LLocalizationCatalogRead(lLocalizationKey)
                ?? lLocalizationFallbackCatalog?.LLocalizationCatalogRead(lLocalizationKey)
                ?? lLocalizationKey;
        }
    }

    internal static string LLocalizationFormat(string lLocalizationKey, params object?[] lLocalizationArguments)
    {
        string lLocalizationTemplate = LLocalizationTextRead(lLocalizationKey);
        try
        {
            return string.Format(CultureInfo.CurrentUICulture, lLocalizationTemplate, lLocalizationArguments);
        }
        catch (FormatException lLocalizationException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Localization format failed for key '{lLocalizationKey}'; showing the raw template.",
                lLocalizationException);
            return lLocalizationTemplate;
        }
    }

    internal static IReadOnlyDictionary<string, string> LLocalizationLanguagesRead()
    {
        lock (lLocalizationGate)
        {
            if (lLocalizationLanguages is not null)
            {
                return lLocalizationLanguages;
            }

            var lLocalizationResult = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string lLocalizationCode in LLocalizationCodesRead())
            {
                LLocalizationCatalog? lLocalizationCatalog = LLocalizationCatalogRead(lLocalizationCode);
                if (lLocalizationCatalog is not null)
                {
                    lLocalizationResult[lLocalizationCatalog.LLocalizationCatalogCode] =
                        lLocalizationCatalog.LLocalizationCatalogName;
                }
            }

            if (lLocalizationResult.Count == 0)
            {
                lLocalizationResult[LLocalizationFallbackCode] = LLocalizationFallbackCode;
            }

            lLocalizationLanguages = lLocalizationResult;
            return lLocalizationLanguages;
        }
    }

    internal static string LLocalizationLanguageRead() =>
        lLocalizationCurrentCatalog?.LLocalizationCatalogCode ?? LLocalizationFallbackCode;

    internal static string LLocalizationLanguageNormalize(string? lLocalizationLanguage)
    {
        if (string.IsNullOrWhiteSpace(lLocalizationLanguage))
        {
            return LLocalizationFallbackCode;
        }

        string lLocalizationValue = lLocalizationLanguage.Trim();
        if (string.Equals(lLocalizationValue, "English", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        if (string.Equals(lLocalizationValue, "Korean", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lLocalizationValue, "한국어", StringComparison.OrdinalIgnoreCase))
        {
            return "ko";
        }

        return Path.GetFileNameWithoutExtension(lLocalizationValue);
    }

    private static IEnumerable<string> LLocalizationCodesRead() =>
        Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Where(lLocalizationName =>
                lLocalizationName.StartsWith(LLocalizationResourcePrefix, StringComparison.Ordinal)
                && lLocalizationName.EndsWith(LLocalizationResourceSuffix, StringComparison.Ordinal))
            .Select(lLocalizationName => lLocalizationName[
                LLocalizationResourcePrefix.Length..^LLocalizationResourceSuffix.Length])
            .OrderBy(lLocalizationCode => lLocalizationCode, StringComparer.OrdinalIgnoreCase);

    private static LLocalizationCatalog? LLocalizationCatalogRead(string lLocalizationCode)
    {
        try
        {
            using Stream? lLocalizationStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                LLocalizationResourcePrefix + lLocalizationCode + LLocalizationResourceSuffix);
            if (lLocalizationStream is null)
            {
                return null;
            }

            using var lLocalizationReader = new StreamReader(lLocalizationStream);
            return LLocalizationCatalog.LLocalizationCatalogLoad(
                lLocalizationCode, lLocalizationReader.ReadToEnd());
        }
        catch (Exception lLocalizationException)
        {
            LTraceLog.LTraceErrorRecord($"Localization could not be loaded: {lLocalizationCode}", lLocalizationException);
            return null;
        }
    }
}
