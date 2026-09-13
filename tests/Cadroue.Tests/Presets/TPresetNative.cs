using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPresetNative
{
    [Fact]
    public void ShippedPresets_ContainExpectedCatalog()
    {
        using TPreset lPresets = new();
        IReadOnlyList<LPresetGroup> lGroups = lPresets.TPresetNativeLoad();

        Assert.Collection(
            lGroups,
            lGroup =>
            {
                Assert.Equal("Default", lGroup.LPresetGroupName);
                Assert.Equal(
                    ["Merge (default)", "Split (default)"],
                    lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
            },
            lGroup =>
            {
                Assert.Equal("General", lGroup.LPresetGroupName);
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
                    lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
            },
            lGroup =>
            {
                Assert.Equal("Hardware", lGroup.LPresetGroupName);
                Assert.Equal(
                    [
                        "Hardware - AV1 AMF",
                        "Hardware - AV1 NVENC",
                        "Hardware - AV1 QSV",
                        "Hardware - H.265 AMF",
                        "Hardware - H.265 NVENC",
                        "Hardware - H.265 QSV"
                    ],
                    lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
            },
            lGroup =>
            {
                Assert.Equal("Matroska", lGroup.LPresetGroupName);
                Assert.Equal(
                    ["Matroska - AV1", "Matroska - H.264", "Matroska - H.265", "Matroska - VP9"],
                    lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
            },
            lGroup =>
            {
                Assert.Equal("Preservation", lGroup.LPresetGroupName);
                Assert.Equal(
                    ["Preservation - FFV1 FLAC", "Preservation - FFV1 Source Audio"],
                    lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
            },
            lGroup =>
            {
                Assert.Equal("Professional", lGroup.LPresetGroupName);
                Assert.Equal(
                    [
                        "Professional - ProRes 422 HQ",
                        "Professional - ProRes 422 LT",
                        "Professional - ProRes 422",
                        "Professional - ProRes Proxy"
                    ],
                    lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
            });
    }

    [Fact]
    public void ShippedPresets_UseExportRecordFormat()
    {
        using TPreset lPresets = new();
        Assert.True(lPresets.TPresetFormatCheck());
    }

    [Fact]
    public void NativeLoad_LoadsEveryJsonPresetInFolder()
    {
        using TPreset lPresets = new();
        string lFolderPath = Path.Combine(Path.GetTempPath(), $"Cadroue-{Guid.NewGuid():N}");
        Directory.CreateDirectory(lFolderPath);
        try
        {
            string lDefaultFolder = Path.Combine(lFolderPath, "Default");
            string lOtherFolder = Path.Combine(lFolderPath, "A");
            Directory.CreateDirectory(lDefaultFolder);
            Directory.CreateDirectory(lOtherFolder);
            lPresets.TPresetNativeSave("First", Path.Combine(lDefaultFolder, "First.json"));
            lPresets.TPresetNativeSave("Second", Path.Combine(lOtherFolder, "Second.json"));
            File.WriteAllText(Path.Combine(lFolderPath, "Ignored.txt"), "not a preset");

            IReadOnlyList<LPresetGroup> lGroups = lPresets.TPresetNativeLoad(lFolderPath);

            Assert.Collection(
                lGroups,
                lGroup =>
                {
                    Assert.Equal("A", lGroup.LPresetGroupName);
                    Assert.Equal(["Second"], lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
                },
                lGroup =>
                {
                    Assert.Equal("Default", lGroup.LPresetGroupName);
                    Assert.Equal(["First"], lGroup.LPresetGroupPresets.Select(lRecord => lRecord.LPresetName));
                });
        }
        finally
        {
            Directory.Delete(lFolderPath, true);
        }
    }
}
