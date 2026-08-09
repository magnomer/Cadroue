using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class SidecarIdentityTests
{
    [Fact]
    public void SidecarFromDifferentSource_IsNotAcceptedAsCurrent()
    {
        using var sidecar = new TSidecar();
        string original = sidecar.SourceCreate("original.mp4", "AAAA");
        string other = sidecar.SourceCreate("other.mp4", "BBBB");
        Assert.True(sidecar.Save(original, TimeSpan.FromSeconds(4), new long[] { 0, 1000 }));
        sidecar.PersistedCopy(original, other);

        Assert.Null(sidecar.Load(other, TimeSpan.FromSeconds(4)));
    }

    [Fact]
    public void ChangedSourceContent_InvalidatesCachedData()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("changing.mp4", "before");
        Assert.True(sidecar.Save(source, TimeSpan.FromSeconds(6), new long[] { 0, 1500 }));

        sidecar.SourceReplace(source, "after!");

        Assert.Null(sidecar.Load(source, TimeSpan.FromSeconds(6)));
    }

    [Fact]
    public void ChangedSourceLength_ParticipatesInValidityCheck()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("resized.mp4", "short");
        Assert.True(sidecar.Save(source, TimeSpan.FromSeconds(3), new long[] { 0, 500 }));

        sidecar.SourceReplace(source, "a substantially longer source");

        Assert.Null(sidecar.Load(source, TimeSpan.FromSeconds(3)));
    }
}
