using Xunit;

namespace Cadroue.Tests;

[Collection("MediaProbe")]
public sealed class MediaAvailabilityTests
{
    [Fact]
    public void MissingSource_ReportsUnavailable()
    {
        using var scout = new TScout();

        Assert.Null(scout.MediaRead(scout.MissingPath("missing.mp4")));
    }

    [Fact]
    public void ExistingSourceSize_IsRead()
    {
        using var scout = new TScout();
        string source = scout.FileCreate("source.mp4", 137);

        Assert.Equal(137, scout.InputBytesRead(source, scout.MissingPath("output.mp4")));
    }

    [Fact]
    public void ExistingOutputSize_IsRead()
    {
        using var scout = new TScout();
        string output = scout.FileCreate("output.mp4", 251);

        Assert.Equal(251, scout.OutputBytesRead(output));
    }

    [Fact]
    public void MissingOutput_ReportsUnknownSize()
    {
        using var scout = new TScout();

        Assert.Null(scout.OutputBytesRead(scout.MissingPath("missing-output.mp4")));
    }
}
