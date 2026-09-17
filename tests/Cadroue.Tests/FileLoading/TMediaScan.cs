using Cadroue.Media;

using Xunit;

namespace Cadroue.Tests;

public sealed class TMediaScan : IDisposable
{
    private readonly string tMediaRoot = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "MediaScan", Guid.NewGuid().ToString("N"));

    public TMediaScan() => Directory.CreateDirectory(tMediaRoot);

    public void Dispose()
    {
        if (Directory.Exists(tMediaRoot))
        {
            Directory.Delete(tMediaRoot, true);
        }
    }

    private string TMediaCreate(string relative)
    {
        string path = Path.Combine(tMediaRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [0]);
        return path;
    }

    [Fact]
    public async Task Scan_KeepsExplicitFilesInRequestOrderAndSortsEachFolder()
    {
        string zebra = TMediaCreate("zebra.mp4");
        string alpha = TMediaCreate("alpha.mkv");
        string nestedB = TMediaCreate(Path.Combine("folder", "b.mp4"));
        string nestedA = TMediaCreate(Path.Combine("folder", "sub", "a.wav"));
        TMediaCreate(Path.Combine("folder", "notes.txt"));

        LMediaScanResult result = await TInterface.TMediaPathScan([zebra, alpha, Path.Combine(tMediaRoot, "folder")]);

        Assert.Equal([zebra, alpha, nestedB, nestedA], result.LMediaScanPaths);
        Assert.Empty(result.LMediaScanNotices);
    }

    [Fact]
    public async Task Scan_SkipsNonMediaFilesAndMissingPaths()
    {
        string text = TMediaCreate("readme.txt");
        string missing = Path.Combine(tMediaRoot, "missing.mp4");

        LMediaScanResult result = await TInterface.TMediaPathScan([text, missing]);

        Assert.Empty(result.LMediaScanPaths);
    }

    [Fact]
    public async Task Scan_HonoursCancellationBeforeWalkingFolders()
    {
        TMediaCreate(Path.Combine("folder", "a.mp4"));
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TInterface.TMediaPathScan([Path.Combine(tMediaRoot, "folder")], source.Token));
    }

    [Fact]
    public async Task Scan_ReturnsSameResultWithAndWithoutFolders()
    {
        string file = TMediaCreate("clip.mp4");
        TMediaCreate(Path.Combine("folder", "clip2.mp4"));

        LMediaScanResult started = await TInterface.TMediaPathScan(
            [file, Path.Combine(tMediaRoot, "folder")], CancellationToken.None);
        LMediaScanResult direct = await TInterface.TMediaPathScan([file, Path.Combine(tMediaRoot, "folder")]);

        Assert.Equal(direct.LMediaScanPaths, started.LMediaScanPaths);
    }
}
