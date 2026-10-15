using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests.Windows;

[CollectionDefinition("Localization", DisableParallelization = true)]
public sealed class TLocalizationCollection;

[Collection("Localization")]
public sealed class TProgramLanguage : IDisposable
{
    private static readonly Dictionary<string, string> tProgramResources = new(StringComparer.Ordinal)
    {
        ["localization.en.json"] = "{\"Terms.App\":\"Cadroue\",\"Localization.Language.Name\":\"English\"}",
        ["localization.xx.json"] = "{\"Terms.App\":\"Cadroue\",\"Localization.Language.Name\":\"Testish\"}",
        ["localization.ffmpeg-error.en.json"] = "{}",
    };

    private readonly LPreferenceState tProgramPrevious = TInterface.TPreferenceCurrentRead();

    public TProgramLanguage() =>
        Interface.TInterface.TLocalizationSeamSet(
            () => tProgramResources.Keys,
            name => tProgramResources.TryGetValue(name, out string? text) ? text : null,
            (_, _) => { });

    public void Dispose()
    {
        TInterface.TPreferenceRestore(tProgramPrevious);
        Interface.TInterface.TLocalizationSeamSet(null, null, null);
    }

    [Fact]
    public void Normalize_RewritesUnavailablePreferenceOnce()
    {
        LPreferenceState draft = Interface.TInterface.TPreferenceClone(tProgramPrevious);
        draft.LPreferenceLanguage = "zz";
        Assert.True(TInterface.TPreferenceRestore(draft));
        Interface.TInterface.TLocalizationLoad("zz");

        Assert.True(TInterface.TProgramLanguageNormalize());
        Assert.Equal("en", TInterface.TPreferenceCurrentRead().LPreferenceLanguage);
        Assert.False(TInterface.TProgramLanguageNormalize());
    }

    [Fact]
    public void Normalize_KeepsAvailablePreference()
    {
        LPreferenceState draft = Interface.TInterface.TPreferenceClone(tProgramPrevious);
        draft.LPreferenceLanguage = "en";
        Assert.True(TInterface.TPreferenceRestore(draft));
        Interface.TInterface.TLocalizationLoad("en");

        Assert.False(TInterface.TProgramLanguageNormalize());
        Assert.Equal("en", TInterface.TPreferenceCurrentRead().LPreferenceLanguage);
    }

    [Fact]
    public void Load_LanguageChangeReachesCatalog()
    {
        TInterface.TProgramLanguageLoad("xx");
        Assert.Equal("Testish", TInterface.TLocalizationCatalogRead("Localization.Language.Name"));

        TInterface.TProgramLanguageLoad("en");
        Assert.Equal("English", TInterface.TLocalizationCatalogRead("Localization.Language.Name"));
    }
}
