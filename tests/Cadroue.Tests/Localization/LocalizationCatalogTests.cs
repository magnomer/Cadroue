using System.Text.Json;

using Xunit;

namespace Cadroue.Tests;

public sealed class LocalizationCatalogTests
{
    [Fact]
    public void EnglishAndKoreanCatalogs_HaveIdenticalKeySets()
    {
        string lLocalizationPath = Path.Combine(RepositoryPathRead(), "localization");
        using JsonDocument lEnglish = JsonDocument.Parse(File.ReadAllText(Path.Combine(lLocalizationPath, "en.json")));
        using JsonDocument lKorean = JsonDocument.Parse(File.ReadAllText(Path.Combine(lLocalizationPath, "ko.json")));
        string[] lEnglishKeys = lEnglish.RootElement.EnumerateObject().Select(lProperty => lProperty.Name).Order().ToArray();
        string[] lKoreanKeys = lKorean.RootElement.EnumerateObject().Select(lProperty => lProperty.Name).Order().ToArray();

        Assert.Equal(lEnglishKeys, lKoreanKeys);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ko")]
    public void Catalog_HasUniqueKeysAndRequiredGroupLabels(string lLocalizationCode)
    {
        string lRepositoryPath = RepositoryPathRead();
        string lLocalizationPath = Path.Combine(lRepositoryPath, "localization", $"{lLocalizationCode}.json");
        using JsonDocument lLocalizationDocument = JsonDocument.Parse(File.ReadAllText(lLocalizationPath));
        var lLocalizationKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (JsonProperty lLocalizationProperty in lLocalizationDocument.RootElement.EnumerateObject())
        {
            Assert.True(
                lLocalizationKeys.Add(lLocalizationProperty.Name),
                $"Duplicate localization key '{lLocalizationProperty.Name}' in {lLocalizationCode}.json");
        }

        Assert.Contains("Group.Manual.Label", lLocalizationKeys);
        Assert.Contains("Group.NumberRemove.Label", lLocalizationKeys);
        Assert.Contains("Group.NumberRemove.Tooltip", lLocalizationKeys);
        foreach (string lGammaKey in GammaKeys)
        {
            Assert.Contains(lGammaKey, lLocalizationKeys);
        }
        foreach (string lWhitebalanceKey in WhitebalanceKeys)
        {
            Assert.Contains(lWhitebalanceKey, lLocalizationKeys);
        }
    }

    private static readonly string[] GammaKeys =
    {
        "Terms.Gamma",
        "Processing.Step.Gamma",
        "Processing.Step.GammaRequiresMpv",
        "Inspector.Step.Gamma",
        "Inspector.Video.ApplyGamma",
        "Inspector.Video.PersistGamma",
        "Inspector.Video.Midtone",
        "Inspector.Video.RedGamma",
        "Inspector.Video.GreenGamma",
        "Inspector.Video.BlueGamma",
        "Inspector.Video.HighlightProtection",
        "Inspector.Video.GammaReset",
        "Inspector.Video.GammaResetTooltip",
        "Inspector.Video.GammaRequiresMpv"
    };

    private static readonly string[] WhitebalanceKeys =
    {
        "Terms.Whitebalance",
        "Processing.Step.Whitebalance",
        "Processing.Step.WhitebalanceRequiresMpv",
        "Inspector.Step.Whitebalance",
        "Inspector.Video.ApplyWhitebalance",
        "Inspector.Video.PersistWhitebalance",
        "Inspector.Video.WhitebalanceMethod",
        "Inspector.Video.WhitebalanceMethodAverage",
        "Inspector.Video.WhitebalanceMethodMinmax",
        "Inspector.Video.WhitebalanceMethodMedian",
        "Inspector.Video.WhitebalanceCancel",
        "Inspector.Video.WhitebalanceGuide",
        "Inspector.Video.WhitebalanceSample",
        "Inspector.Video.WhitebalanceInvalid",
        "Inspector.Video.WhitebalanceDecode",
        "Inspector.Video.WhitebalanceSaturation",
        "Inspector.Video.WhitebalanceWarning",
        "Inspector.Video.WhitebalanceReset",
        "Inspector.Video.WhitebalanceResetTooltip",
        "Inspector.Video.WhitebalanceRequiresMpv"
    };

    private static string RepositoryPathRead()
    {
        DirectoryInfo? lDirectory = new(AppContext.BaseDirectory);
        while (lDirectory is not null && !File.Exists(Path.Combine(lDirectory.FullName, "Cadroue.sln")))
        {
            lDirectory = lDirectory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(lDirectory).FullName;
    }
}
