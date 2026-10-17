using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cadroue.UIDeportment;

public sealed class QLocalizationCatalog : INotifyPropertyChanged
{
    private static readonly ConditionalWeakTable<ResourceDictionary, ResourceDictionary> qLocalizationCatalogMerged =
        [];

    private IReadOnlyDictionary<string, string> qLocalizationCatalogTexts =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private IReadOnlyDictionary<string, string> qLocalizationCatalogFallback =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private Action<string, Exception?> qLocalizationCatalogTrace = (_, _) => { };

    public static QLocalizationCatalog QLocalizationCatalogCurrent { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => QLocalizationTextFind(key) ?? string.Empty;

    public static string QLocalizationTextRead(string key)
    {
        return QLocalizationTextFind(key) ?? key;
    }

    public static string? QLocalizationTextFind(string key)
    {
        return QLocalizationCatalogCurrent.qLocalizationCatalogTexts.TryGetValue(key, out string? text)
            ? text
            : null;
    }

    public static string QLocalizationFormat(string key, params object?[] arguments)
    {
        string? selected = QLocalizationTextFind(key);
        string? fallback = QLocalizationFallbackFind(key);
        string template = selected ?? key;
        try
        {
            return string.Format(CultureInfo.CurrentUICulture, template, arguments);
        }
        catch (FormatException exception)
        {
            Action<string, Exception?> trace = QLocalizationCatalogCurrent.qLocalizationCatalogTrace;
            if (selected is not null
                && fallback is not null
                && !string.Equals(selected, fallback, StringComparison.Ordinal))
            {
                try
                {
                    string repaired = string.Format(CultureInfo.CurrentUICulture, fallback, arguments);
                    trace($"Localization format failed for key '{key}'; using the English template.", exception);
                    return repaired;
                }
                catch (FormatException fallbackException)
                {
                    trace($"English localization format also failed for key '{key}'.", fallbackException);
                }
            }

            trace($"Localization format failed for key '{key}'; showing the raw template.", exception);
            return template;
        }
    }

    internal static void QLocalizationCatalogApply(
        ResourceDictionary? resources,
        IReadOnlyDictionary<string, string> texts,
        IReadOnlyDictionary<string, string> fallback,
        Action<string, Exception?> trace)
    {
        QLocalizationCatalogSet(texts, fallback, trace);
        if (QLocalizationStaleFind(resources, texts) is { } stale)
        {
            QLocalizationResourceApply(stale, texts);
        }

        QLocalizationCatalogCurrent.PropertyChanged?.Invoke(
            QLocalizationCatalogCurrent,
            new PropertyChangedEventArgs("Item[]"));
    }

    private static void QLocalizationCatalogSet(
        IReadOnlyDictionary<string, string> texts,
        IReadOnlyDictionary<string, string> fallback,
        Action<string, Exception?> trace)
    {
        QLocalizationCatalogCurrent.qLocalizationCatalogTexts = texts;
        QLocalizationCatalogCurrent.qLocalizationCatalogFallback = fallback;
        QLocalizationCatalogCurrent.qLocalizationCatalogTrace = trace;
    }

    private static ResourceDictionary? QLocalizationStaleFind(
        ResourceDictionary? resources, IReadOnlyDictionary<string, string> texts) =>
        resources is not null && !texts.All(pair => Equals(resources[pair.Key], pair.Value)) ? resources : null;

    private static string? QLocalizationFallbackFind(string key)
    {
        return QLocalizationCatalogCurrent.qLocalizationCatalogFallback.TryGetValue(key, out string? text)
            ? text
            : null;
    }

    private static void QLocalizationResourceApply(
        ResourceDictionary resources, IReadOnlyDictionary<string, string> texts)
    {
        ResourceDictionary catalog = new();
        qLocalizationCatalogMerged.TryGetValue(resources, out ResourceDictionary? previous);
        if (previous is not null)
        {
            foreach (DictionaryEntry entry in previous)
            {
                catalog[entry.Key] = entry.Value;
            }
        }

        foreach ((string key, string value) in texts)
        {
            catalog[key] = value;
        }

        int index = previous is null ? -1 : resources.MergedDictionaries.IndexOf(previous);
        if (index < 0)
        {
            resources.MergedDictionaries.Add(catalog);
        }
        else
        {
            resources.MergedDictionaries[index] = catalog;
        }

        qLocalizationCatalogMerged.AddOrUpdate(resources, catalog);
    }
}
