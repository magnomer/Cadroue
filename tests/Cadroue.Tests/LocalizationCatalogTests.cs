using System.Text.Json;

using Xunit;

namespace Cadroue.Tests;

public sealed class LocalizationCatalogTests
{
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
    }

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
