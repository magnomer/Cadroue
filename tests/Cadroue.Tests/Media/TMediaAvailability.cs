using Xunit;

namespace Cadroue.Tests;

[Collection("MediaProbe")]
public sealed class TMediaAvailability
{
    [Fact]
    public void MissingSource_ReportsUnavailable()
    {
        using var scout = new TScout();

        Assert.Null(scout.TMediaRead(scout.TMediaMissingRead("missing.mp4")));
    }

    [Fact]
    public void ExistingSourceSize_IsRead()
    {
        using var scout = new TScout();
        string source = scout.TScoutFileCreate("source.mp4", 137);

        Assert.Equal(137, scout.TScoutInputRead(source, scout.TMediaMissingRead("output.mp4")));
    }

    [Fact]
    public void ExistingOutputSize_IsRead()
    {
        using var scout = new TScout();
        string output = scout.TScoutFileCreate("output.mp4", 251);

        Assert.Equal(251, scout.TOutputBytesRead(output));
    }

    [Fact]
    public void MissingOutput_ReportsUnknownSize()
    {
        using var scout = new TScout();

        Assert.Null(scout.TOutputBytesRead(scout.TMediaMissingRead("missing-output.mp4")));
    }
}
