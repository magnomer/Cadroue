using Xunit;

namespace Cadroue.Tests;

[Collection("Localization")]
public sealed class TLocalizationSeam : IDisposable
{
    private readonly List<string> tLocalizationTraced = [];

    private static readonly Dictionary<string, string> tLocalizationResources = new(StringComparer.Ordinal)
    {
        ["localization.en.json"] =
            "{\"Terms.App\":\"Cadroue\",\"Localization.Language.Name\":\"English\",\"Sample.Greeting\":\"Hello\"}",
        ["localization.xx.json"] =
            "{\"Terms.App\":\"Cadroue\",\"Localization.Language.Name\":\"Testish\",\"Sample.Greeting\":\"Yo\"}",
        ["localization.ffmpeg-error.en.json"] = "{}",
    };

    public TLocalizationSeam() =>
        TInterface.TLocalizationSeamSet(
            () => tLocalizationResources.Keys,
            name => tLocalizationResources.TryGetValue(name, out string? text) ? text : null,
            (message, _) => tLocalizationTraced.Add(message));

    public void Dispose() => TInterface.TLocalizationSeamSet(null, null, null);

    [Fact]
    public void Load_ReadsCatalogsThroughInjectedProvider()
    {
        TInterface.TLocalizationLoad("xx");

        Assert.Equal("xx", TInterface.TLocalizationLanguageRead());
        Assert.Equal("Yo", TInterface.TLocalizationTextRead("Sample.Greeting"));
        Assert.Equal(["en", "xx"], TInterface.TLocalizationLanguagesRead().Keys);
        Assert.Empty(tLocalizationTraced);
    }

    [Fact]
    public void Load_UnknownLanguageFallsBackToEnglishAndTraces()
    {
        TInterface.TLocalizationLoad("zz");

        Assert.Equal("en", TInterface.TLocalizationLanguageRead());
        Assert.Equal("Hello", TInterface.TLocalizationTextRead("Sample.Greeting"));
        Assert.Contains(tLocalizationTraced, message => message.Contains("'zz' is unavailable", StringComparison.Ordinal));
    }
}
