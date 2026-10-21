using System.Reflection;
using Cadroue.UIDeportment;
using Xunit;

namespace Cadroue.Tests.Windows;

[Collection(nameof(TLocalizationCatalogCollection))]
public sealed class TLocalizationChoice
{
    [Fact]
    public void CurrentText_ChangesAfterConstruction()
    {
        const string key = "Inspector.Video.CurveChannelMaster";
        var choice = new QLocalizationChoice(key);
        QLocalizationCatalog catalog = QLocalizationCatalog.QLocalizationCatalogCurrent;
        Type catalogType = typeof(QLocalizationCatalog);
        FieldInfo textsField = catalogType.GetField(
            "qLocalizationCatalogTexts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo fallbackField = catalogType.GetField(
            "qLocalizationCatalogFallback", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo traceField = catalogType.GetField(
            "qLocalizationCatalogTrace", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var priorTexts = (IReadOnlyDictionary<string, string>)textsField.GetValue(catalog)!;
        var priorFallback = (IReadOnlyDictionary<string, string>)fallbackField.GetValue(catalog)!;
        var priorTrace = (Action<string, Exception?>)traceField.GetValue(catalog)!;

        try
        {
            QLocalizationCatalog.QLocalizationCatalogApply(
                null,
                new Dictionary<string, string> { [key] = "Current one" },
                new Dictionary<string, string> { [key] = "Fallback" },
                (_, _) => { });

            Assert.Equal("Current one", choice.ToString());

            QLocalizationCatalog.QLocalizationCatalogApply(
                null,
                new Dictionary<string, string> { [key] = "Current two" },
                new Dictionary<string, string> { [key] = "Fallback" },
                (_, _) => { });

            Assert.Equal("Current two", choice.ToString());
        }
        finally
        {
            QLocalizationCatalog.QLocalizationCatalogApply(
                null,
                priorTexts,
                priorFallback,
                priorTrace);
        }
    }

    [Fact]
    public void MissingCurrent_UsesFallbackThenKey()
    {
        const string key = "Inspector.Video.CurveChannelMaster";
        var choice = new QLocalizationChoice(key);
        QLocalizationCatalog catalog = QLocalizationCatalog.QLocalizationCatalogCurrent;
        Type catalogType = typeof(QLocalizationCatalog);
        FieldInfo textsField = catalogType.GetField(
            "qLocalizationCatalogTexts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo fallbackField = catalogType.GetField(
            "qLocalizationCatalogFallback", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo traceField = catalogType.GetField(
            "qLocalizationCatalogTrace", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var priorTexts = (IReadOnlyDictionary<string, string>)textsField.GetValue(catalog)!;
        var priorFallback = (IReadOnlyDictionary<string, string>)fallbackField.GetValue(catalog)!;
        var priorTrace = (Action<string, Exception?>)traceField.GetValue(catalog)!;

        Assert.Throws<ArgumentNullException>(() => new QLocalizationChoice(null!));
        Assert.Throws<ArgumentException>(() => new QLocalizationChoice(string.Empty));

        try
        {
            QLocalizationCatalog.QLocalizationCatalogApply(
                null,
                new Dictionary<string, string>(),
                new Dictionary<string, string> { [key] = "Fallback" },
                (_, _) => { });

            Assert.Equal("Fallback", choice.ToString());

            QLocalizationCatalog.QLocalizationCatalogApply(
                null,
                new Dictionary<string, string>(),
                new Dictionary<string, string>(),
                (_, _) => { });

            Assert.Equal(key, choice.ToString());
        }
        finally
        {
            QLocalizationCatalog.QLocalizationCatalogApply(
                null,
                priorTexts,
                priorFallback,
                priorTrace);
        }
    }
}

[CollectionDefinition(nameof(TLocalizationCatalogCollection), DisableParallelization = true)]
public sealed class TLocalizationCatalogCollection
{
}
