using System.Globalization;

namespace Cadroue.Application;

public static class LLocalization
{
    private const string LLocalizationFallbackCode = "en";

    public static Action<string, Exception?>? LLocalizationTraceSeam { get; set; }

    private static readonly object lLocalizationGate = new();
    private static LLocalizationCatalog? lLocalizationFallbackCatalog;
    private static LLocalizationCatalog? lLocalizationCurrentCatalog;
    private static IReadOnlyDictionary<string, string>? lLocalizationLanguages;

    public static void LLocalizationLoad(string? lLocalizationLanguage)
    {
        lock (lLocalizationGate)
        {
            lLocalizationFallbackCatalog = LLocalizationResource.LLocalizationCatalogRead(LLocalizationFallbackCode);

            if (lLocalizationFallbackCatalog is null)
            {
                LLocalizationTraceRecord(
                    "English localization could not be loaded from the program resources; "
                    + "keys will show untranslated.");
            }

            string lLocalizationCode = LLocalizationLanguageNormalize(lLocalizationLanguage);
            if (string.Equals(lLocalizationCode, LLocalizationFallbackCode, StringComparison.OrdinalIgnoreCase))
            {
                lLocalizationCurrentCatalog = lLocalizationFallbackCatalog;
            }
            else
            {
                LLocalizationCatalog? lLocalizationSelected =
                    LLocalizationResource.LLocalizationCatalogRead(lLocalizationCode);
                if (lLocalizationSelected is null)
                {
                    LLocalizationTraceRecord(
                        $"Localization '{lLocalizationCode}' is unavailable; using {LLocalizationFallbackCode}.");
                }

                lLocalizationCurrentCatalog = lLocalizationSelected ?? lLocalizationFallbackCatalog;
            }

            lLocalizationLanguages = null;
            LLocalizationCultureApply(
                lLocalizationCurrentCatalog?.LLocalizationCatalogCode ?? LLocalizationFallbackCode);
        }
    }

    private static void LLocalizationCultureApply(string lLocalizationCode)
    {
        CultureInfo lLocalizationCulture;
        try
        {
            lLocalizationCulture = CultureInfo.GetCultureInfo(lLocalizationCode);
        }
        catch (CultureNotFoundException)
        {
            lLocalizationCulture = CultureInfo.GetCultureInfo(LLocalizationFallbackCode);
        }

        CultureInfo.CurrentUICulture = lLocalizationCulture;
        CultureInfo.DefaultThreadCurrentUICulture = lLocalizationCulture;
    }

    public static IReadOnlyDictionary<string, string> LLocalizationTextsRead()
    {
        lock (lLocalizationGate)
        {
            var lLocalizationTexts = new Dictionary<string, string>(
                LLocalizationFallbackRead(), StringComparer.Ordinal);
            foreach ((string lLocalizationKey, string lLocalizationText)
                in lLocalizationCurrentCatalog?.LLocalizationCatalogValues ?? LLocalizationFallbackRead())
            {
                lLocalizationTexts[lLocalizationKey] = lLocalizationText;
            }

            return lLocalizationTexts;
        }
    }

    public static IReadOnlyDictionary<string, string> LLocalizationFallbackRead()
    {
        lock (lLocalizationGate)
        {
            return lLocalizationFallbackCatalog?.LLocalizationCatalogValues
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    public static string LLocalizationTextRead(string lLocalizationKey)
    {
        lock (lLocalizationGate)
        {
            return lLocalizationCurrentCatalog?.LLocalizationCatalogRead(lLocalizationKey)
                ?? lLocalizationFallbackCatalog?.LLocalizationCatalogRead(lLocalizationKey)
                ?? lLocalizationKey;
        }
    }

    public static string LLocalizationFormat(string lLocalizationKey, params object?[] lLocalizationArguments)
    {
        string? lLocalizationSelectedTemplate;
        string? lLocalizationFallbackTemplate;
        lock (lLocalizationGate)
        {
            lLocalizationSelectedTemplate = lLocalizationCurrentCatalog?.LLocalizationCatalogRead(lLocalizationKey);
            lLocalizationFallbackTemplate = lLocalizationFallbackCatalog?.LLocalizationCatalogRead(lLocalizationKey);
        }

        string lLocalizationTemplate = lLocalizationSelectedTemplate
            ?? lLocalizationFallbackTemplate
            ?? lLocalizationKey;
        try
        {
            return string.Format(CultureInfo.CurrentUICulture, lLocalizationTemplate, lLocalizationArguments);
        }
        catch (FormatException lLocalizationException)
        {
            if (lLocalizationSelectedTemplate is not null
                && lLocalizationFallbackTemplate is not null
                && !string.Equals(
                    lLocalizationSelectedTemplate,
                    lLocalizationFallbackTemplate,
                    StringComparison.Ordinal))
            {
                try
                {
                    string lLocalizationFallback = string.Format(
                        CultureInfo.CurrentUICulture,
                        lLocalizationFallbackTemplate,
                        lLocalizationArguments);
                    LLocalizationTraceRecord(
                        $"Localization format failed for key '{lLocalizationKey}'; using the English template.",
                        lLocalizationException);
                    return lLocalizationFallback;
                }
                catch (FormatException lLocalizationFallbackException)
                {
                    LLocalizationTraceRecord(
                        $"English localization format also failed for key '{lLocalizationKey}'.",
                        lLocalizationFallbackException);
                }
            }

            LLocalizationTraceRecord(
                $"Localization format failed for key '{lLocalizationKey}'; showing the raw template.",
                lLocalizationException);
            return lLocalizationTemplate;
        }
    }

    public static IReadOnlyDictionary<string, string> LLocalizationLanguagesRead()
    {
        lock (lLocalizationGate)
        {
            if (lLocalizationLanguages is not null)
            {
                return lLocalizationLanguages;
            }

            var lLocalizationResult = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string lLocalizationCode in LLocalizationResource.LLocalizationCodesRead())
            {
                LLocalizationCatalog? lLocalizationCatalog =
                    LLocalizationResource.LLocalizationCatalogRead(lLocalizationCode);
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

    public static string LLocalizationLanguageRead() =>
        lLocalizationCurrentCatalog?.LLocalizationCatalogCode ?? LLocalizationFallbackCode;

    public static string LLocalizationLanguageNormalize(string? lLocalizationLanguage)
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

    public static void LLocalizationTraceRecord(
        string lLocalizationMessage,
        Exception? lLocalizationException = null) =>
        LLocalizationTraceSeam?.Invoke(lLocalizationMessage, lLocalizationException);
}
