using Xunit;

namespace Cadroue.Tests;

public sealed class PresetNativeTests
{
    [Fact]
    public void ShippedPresets_ContainExpectedCatalog()
    {
        using TPresets lPresets = new();
        IReadOnlyList<(string Name, IReadOnlyList<string> Presets)> lGroups = lPresets.NativeLoad();

        Assert.Collection(
            lGroups,
            lGroup =>
            {
                Assert.Equal("Default", lGroup.Name);
                Assert.Equal(["Merge (default)", "Split (default)"], lGroup.Presets);
            },
            lGroup =>
            {
                Assert.Equal("General", lGroup.Name);
                Assert.Equal(
                    [
                        "General - AV1 Balanced",
                        "General - AV1 High Quality",
                        "General - H.264 Balanced",
                        "General - H.264 Fast",
                        "General - H.264 High Quality",
                        "General - H.265 Balanced",
                        "General - H.265 Fast",
                        "General - H.265 High Quality"
                    ],
                    lGroup.Presets);
            },
            lGroup =>
            {
                Assert.Equal("Hardware", lGroup.Name);
                Assert.Equal(
                    [
                        "Hardware - AV1 AMF",
                        "Hardware - AV1 NVENC",
                        "Hardware - AV1 QSV",
                        "Hardware - H.265 AMF",
                        "Hardware - H.265 NVENC",
                        "Hardware - H.265 QSV"
                    ],
                    lGroup.Presets);
            },
            lGroup =>
            {
                Assert.Equal("Matroska", lGroup.Name);
                Assert.Equal(
                    ["Matroska - AV1", "Matroska - H.264", "Matroska - H.265", "Matroska - VP9"],
                    lGroup.Presets);
            },
            lGroup =>
            {
                Assert.Equal("Preservation", lGroup.Name);
                Assert.Equal(
                    ["Preservation - FFV1 FLAC", "Preservation - FFV1 Source Audio"],
                    lGroup.Presets);
            },
            lGroup =>
            {
                Assert.Equal("Professional", lGroup.Name);
                Assert.Equal(
                    [
                        "Professional - ProRes 422 HQ",
                        "Professional - ProRes 422 LT",
                        "Professional - ProRes 422",
                        "Professional - ProRes Proxy"
                    ],
                    lGroup.Presets);
            });
    }

    [Fact]
    public void ShippedPresets_UseExportRecordFormat()
    {
        using TPresets lPresets = new();
        Assert.True(lPresets.NativeFormatValid());
    }

    [Fact]
    public void NativeLoad_LoadsEveryJsonPresetInFolder()
    {
        using TPresets lPresets = new();
        string lFolderPath = Path.Combine(Path.GetTempPath(), $"Cadroue-{Guid.NewGuid():N}");
        Directory.CreateDirectory(lFolderPath);
        try
        {
            string lDefaultFolder = Path.Combine(lFolderPath, "Default");
            string lOtherFolder = Path.Combine(lFolderPath, "A");
            Directory.CreateDirectory(lDefaultFolder);
            Directory.CreateDirectory(lOtherFolder);
            lPresets.NativeSave("First", Path.Combine(lDefaultFolder, "First.json"));
            lPresets.NativeSave("Second", Path.Combine(lOtherFolder, "Second.json"));
            File.WriteAllText(Path.Combine(lFolderPath, "Ignored.txt"), "not a preset");

            IReadOnlyList<(string Name, IReadOnlyList<string> Presets)> lGroups = lPresets.NativeLoad(lFolderPath);

            Assert.Collection(
                lGroups,
                lGroup =>
                {
                    Assert.Equal("A", lGroup.Name);
                    Assert.Equal(["Second"], lGroup.Presets);
                },
                lGroup =>
                {
                    Assert.Equal("Default", lGroup.Name);
                    Assert.Equal(["First"], lGroup.Presets);
                });
        }
        finally
        {
            Directory.Delete(lFolderPath, true);
        }
    }
}
