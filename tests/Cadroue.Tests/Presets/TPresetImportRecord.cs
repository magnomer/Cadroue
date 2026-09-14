using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TPresetImportRecord
{
    [Fact]
    public void ExplicitNullFields_ReceiveDefaults()
    {
        using TPreset presets = new();
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-import-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{"LPresetName":null,"LPresetVideo":null,"LPresetAudio":{"LPresetExtras":null}}""");
        try
        {
            LPresetRecord? record = presets.TPresetFileLoad(path);

            Assert.NotNull(record);
            Assert.NotNull(record.LPresetName);
            Assert.NotNull(record.LPresetVideo);
            Assert.NotNull(record.LPresetAudio.LPresetExtras);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EmptyObject_LoadsDefaultRecord()
    {
        using TPreset presets = new();
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-import-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{}");
        try
        {
            LPresetRecord? record = presets.TPresetFileLoad(path);

            Assert.NotNull(record);
            Assert.Equal(presets.TPresetDefaultCreate().LPresetContainer, record.LPresetContainer);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExportToTheCataloguePath_IsRefused()
    {
        using TPreset presets = new();
        string catalogue = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cadroue",
            "LExportSpecificPresets.json");
        string sibling = Path.Combine(Path.GetTempPath(), $"cadroue-export-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(presets.TPresetCatalogCheck(catalogue));
            Assert.True(presets.TPresetCatalogCheck(catalogue.ToUpperInvariant()));
            Assert.False(presets.TPresetNativeSave("Trap", catalogue));
            Assert.False(presets.TPresetCatalogCheck(sibling));
            Assert.True(presets.TPresetNativeSave("Safe", sibling));
        }
        finally
        {
            File.Delete(sibling);
        }
    }
}
