using System.IO;
using System.Globalization;

namespace Cadroue.UIShell;

internal static class LLocalization
{
    private const string LLocalizationFolderName = "localization";
    private const string LLocalizationFallbackCode = "en";

    private static readonly object lLocalizationGate = new();
    private static LLocalizationCatalog? lLocalizationFallbackCatalog;
    private static LLocalizationCatalog? lLocalizationCurrentCatalog;
    private static IReadOnlyDictionary<string, string>? lLocalizationLanguages;

    internal static void LLocalizationLoad(string? lLocalizationLanguage)
    {
        lock (lLocalizationGate)
        {
            string lLocalizationFolder = LLocalizationFolderRead();
            lLocalizationFallbackCatalog = LLocalizationCatalogRead(
                Path.Combine(lLocalizationFolder, $"{LLocalizationFallbackCode}.json"));

            if (lLocalizationFallbackCatalog is null)
            {
                LAppLog.LError(
                    $"English localization could not be loaded from '{lLocalizationFolder}'; keys will show untranslated.");
            }

            string lLocalizationCode = LLocalizationLanguageNormalize(lLocalizationLanguage);
            string lLocalizationPath = Path.Combine(lLocalizationFolder, $"{lLocalizationCode}.json");
            if (string.Equals(lLocalizationCode, LLocalizationFallbackCode, StringComparison.OrdinalIgnoreCase))
            {
                lLocalizationCurrentCatalog = lLocalizationFallbackCatalog;
            }
            else
            {
                LLocalizationCatalog? lLocalizationSelected = LLocalizationCatalogRead(lLocalizationPath);
                if (lLocalizationSelected is null)
                {
                    LAppLog.LError(
                        $"Localization '{lLocalizationCode}' is unavailable ('{lLocalizationPath}'); using {LLocalizationFallbackCode}.");
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
            LAppLog.LError(
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
            string lLocalizationFolder = LLocalizationFolderRead();
            if (Directory.Exists(lLocalizationFolder))
            {
                foreach (string lLocalizationPath in Directory.EnumerateFiles(lLocalizationFolder, "*.json")
                             .OrderBy(lLocalizationPath => lLocalizationPath, StringComparer.OrdinalIgnoreCase))
                {
                    LLocalizationCatalog? lLocalizationCatalog = LLocalizationCatalogRead(lLocalizationPath);
                    if (lLocalizationCatalog is not null)
                    {
                        lLocalizationResult[lLocalizationCatalog.LLocalizationCatalogCode] =
                            lLocalizationCatalog.LLocalizationCatalogName;
                    }
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

    private static string LLocalizationFolderRead() =>
        Path.Combine(AppContext.BaseDirectory, LLocalizationFolderName);

    private static LLocalizationCatalog? LLocalizationCatalogRead(string lLocalizationPath)
    {
        if (!File.Exists(lLocalizationPath))
        {
            return null;
        }

        try
        {
            return LLocalizationCatalog.LLocalizationCatalogLoad(lLocalizationPath);
        }
        catch (Exception lLocalizationException)
        {
            LAppLog.LError($"Localization file could not be loaded: {lLocalizationPath}", lLocalizationException);
            return null;
        }
    }
}
