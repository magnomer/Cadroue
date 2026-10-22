using Cadroue.Infrastructure;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

public sealed class TSectionPalettePersistence : IDisposable
{
    private readonly string tSectionFolder = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "SectionPalette", Guid.NewGuid().ToString("N"));

    public TSectionPalettePersistence() => Directory.CreateDirectory(tSectionFolder);

    public void Dispose()
    {
        if (Directory.Exists(tSectionFolder))
        {
            Directory.Delete(tSectionFolder, true);
        }
    }

    [Fact]
    public void SaveThenLoad_RoundTripsNameAndColors()
    {
        string path = Path.Combine(tSectionFolder, "Warm.json");

        Assert.True(TInterface.TSectionPaletteSave(path, "Warm", ["#FF0000", "#00FF00"]));
        IReadOnlyList<LSectionPaletteFile> loaded = TInterface.TSectionPaletteLoad(tSectionFolder);

        LSectionPaletteFile palette = Assert.Single(loaded);
        Assert.Equal("Warm", palette.LSectionPaletteName);
        Assert.Equal(["#FF0000", "#00FF00"], palette.LSectionPaletteColors);
        Assert.Equal(path, palette.LSectionPalettePath);
    }

    [Fact]
    public void Load_SkipsHiddenListAndInvalidFiles()
    {
        File.WriteAllText(Path.Combine(tSectionFolder, ".hidden.json"), "[\"Muted\"]");
        File.WriteAllText(Path.Combine(tSectionFolder, "broken.json"), "{ not json");
        File.WriteAllText(Path.Combine(tSectionFolder, "empty.json"), "{\"Name\":\"Empty\",\"Colors\":[]}");
        File.WriteAllText(Path.Combine(tSectionFolder, "ok.json"), "{\"Name\":\" Ok \",\"Colors\":[\"#123456\"]}");

        IReadOnlyList<LSectionPaletteFile> loaded = TInterface.TSectionPaletteLoad(tSectionFolder);

        LSectionPaletteFile palette = Assert.Single(loaded);
        Assert.Equal("Ok", palette.LSectionPaletteName);
    }

    [Fact]
    public void HiddenList_RoundTrips()
    {
        Assert.Empty(TInterface.TSectionHiddenLoad(tSectionFolder));

        Assert.True(TInterface.TSectionHiddenSave(tSectionFolder, ["Vivid"]));
        Assert.True(TInterface.TSectionPaletteRemove(tSectionFolder, "Muted", true, null));
        Assert.True(TInterface.TSectionPaletteRemove(tSectionFolder, "Muted", true, null));

        Assert.Equal(["Vivid", "Muted"], TInterface.TSectionHiddenLoad(tSectionFolder));
        Assert.Empty(TInterface.TSectionPaletteLoad(tSectionFolder));
    }

    [Fact]
    public void Import_CopiesIntoFolderAndAvoidsNameCollision()
    {
        string source = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", $"palette-{Guid.NewGuid():N}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllText(source, "{\"Name\":\"Cool\",\"Colors\":[\"#0000FF\"]}");
        try
        {
            LSectionImportResult first =
                TInterface.TSectionPaletteImport(tSectionFolder, source, out string firstPath);
            LSectionImportResult second =
                TInterface.TSectionPaletteImport(tSectionFolder, source, out string secondPath);

            Assert.Equal(LSectionImportResult.LSectionImportCopied, first);
            Assert.Equal(LSectionImportResult.LSectionImportCopied, second);
            Assert.Equal(Path.Combine(tSectionFolder, "Cool.json"), firstPath);
            Assert.Equal(Path.Combine(tSectionFolder, "Cool 2.json"), secondPath);
            Assert.True(File.Exists(secondPath));
            Assert.Equal(2, TInterface.TSectionPaletteLoad(tSectionFolder).Count);
        }
        finally
        {
            File.Delete(source);
        }
    }

    [Fact]
    public void Import_ReservedNameIsRefusedAndInsideFolderIsNotCopied()
    {
        string inside = Path.Combine(tSectionFolder, "Inside.json");
        string invalid = Path.Combine(tSectionFolder, "Invalid.json");
        string blocked = Path.Combine(tSectionFolder, "blocked");
        File.WriteAllText(inside, "{\"Name\":\"Inside\",\"Colors\":[\"#0000FF\"]}");
        File.WriteAllText(invalid, "{ invalid");
        File.WriteAllText(blocked, "occupied");

        LSectionImportResult reserved = TInterface.TSectionPaletteImport(tSectionFolder, inside, ".hidden", out _);
        LSectionImportResult refused =
            TInterface.TSectionPaletteImport(tSectionFolder, invalid, out string refusedPath);
        LSectionImportResult kept =
            TInterface.TSectionPaletteImport(tSectionFolder, inside, out string keptPath);
        LSectionImportResult failed =
            TInterface.TSectionPaletteImport(blocked, inside, out string failedPath);

        Assert.Equal(LSectionImportResult.LSectionImportReserved, reserved);
        Assert.Equal(LSectionImportResult.LSectionImportInvalid, refused);
        Assert.Equal(string.Empty, refusedPath);
        Assert.Equal(LSectionImportResult.LSectionImportCopied, kept);
        Assert.Equal(inside, keptPath);
        Assert.Equal(LSectionImportResult.LSectionImportFailed, failed);
        Assert.Equal(Path.Combine(blocked, "Inside.json"), failedPath);
        Assert.Equal(3, Directory.GetFiles(tSectionFolder).Length);
    }

    [Fact]
    public void Delete_RemovesFileAndReportsMissing()
    {
        string path = Path.Combine(tSectionFolder, "Gone.json");
        File.WriteAllText(path, "{\"Name\":\"Gone\",\"Colors\":[\"#000000\"]}");

        Assert.True(TInterface.TSectionPaletteRemove(null, "Gone", false, path));
        Assert.False(File.Exists(path));
        Assert.Null(TInterface.TSectionPaletteRead(path));
        Assert.False(TInterface.TSectionPaletteRemove(null, "Gone", false, null));
    }
}
