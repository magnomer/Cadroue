using Xunit;

namespace Cadroue.Tests.Engine;

public sealed class TRendererFacts : IDisposable
{
    private readonly string tRendererFactsFolder = Path.Combine(
        Path.GetTempPath(), "Cadroue.Tests", "RendererFacts", Guid.NewGuid().ToString("N"));

    public TRendererFacts() => Directory.CreateDirectory(tRendererFactsFolder);

    public void Dispose()
    {
        if (Directory.Exists(tRendererFactsFolder))
        {
            Directory.Delete(tRendererFactsFolder, true);
        }
    }

    [Fact]
    public void LRendererFactsRead()
    {
        Assert.Equal((false, false), TInterfaceEngine.TRendererReadinessRead("  "));
        Assert.Equal(
            (false, false),
            TInterfaceEngine.TRendererReadinessRead(Path.Combine(tRendererFactsFolder, "missing")));
        Assert.Equal(
            (false, false), TInterfaceEngine.TRendererReadinessRead(tRendererFactsFolder));

        File.WriteAllText(Path.Combine(tRendererFactsFolder, "ffmpeg.exe"), string.Empty);
        Assert.Equal(
            (true, false), TInterfaceEngine.TRendererReadinessRead(tRendererFactsFolder));

        foreach (string name in new[]
                 {
                     "avcodec-1.dll", "avformat-1.dll", "avutil-1.dll", "swscale-1.dll", "swresample-1.dll"
                 })
        {
            File.WriteAllText(Path.Combine(tRendererFactsFolder, name), string.Empty);
        }

        Assert.Equal(
            (true, true), TInterfaceEngine.TRendererReadinessRead(tRendererFactsFolder));
        File.Delete(Path.Combine(tRendererFactsFolder, "ffmpeg.exe"));
        Assert.Equal(
            (false, true), TInterfaceEngine.TRendererReadinessRead(tRendererFactsFolder));

        string nested = Path.Combine(tRendererFactsFolder, "nested");
        Directory.CreateDirectory(nested);
        foreach (string path in Directory.GetFiles(tRendererFactsFolder, "*.dll"))
        {
            File.Move(path, Path.Combine(nested, Path.GetFileName(path)));
        }

        Assert.Equal(
            (false, false), TInterfaceEngine.TRendererReadinessRead(tRendererFactsFolder));
    }
}
