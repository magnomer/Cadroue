using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cadroue.UIDeportment;

// The catalog's texts mirrored into a resource dictionary: one merged dictionary per resource set, replaced in place
// whenever the texts it shows are stale.
internal static class QLocalizationDictionary
{
    private static readonly ConditionalWeakTable<ResourceDictionary, ResourceDictionary> qLocalizationDictionaryMerged =
        [];

    internal static void QLocalizationDictionaryApply(
        ResourceDictionary? resources, IReadOnlyDictionary<string, string> texts)
    {
        if (QLocalizationStaleFind(resources, texts) is { } stale)
        {
            QLocalizationResourceApply(stale, texts);
        }
    }

    private static ResourceDictionary? QLocalizationStaleFind(
        ResourceDictionary? resources, IReadOnlyDictionary<string, string> texts) =>
        resources is not null && !texts.All(pair => Equals(resources[pair.Key], pair.Value)) ? resources : null;

    private static void QLocalizationResourceApply(
        ResourceDictionary resources, IReadOnlyDictionary<string, string> texts)
    {
        ResourceDictionary catalog = new();
        qLocalizationDictionaryMerged.TryGetValue(resources, out ResourceDictionary? previous);
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

        qLocalizationDictionaryMerged.AddOrUpdate(resources, catalog);
    }
}
