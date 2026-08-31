using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TSidecarIdentity
{
    [Fact]
    public void SidecarFromDifferentSource_IsNotAcceptedAsCurrent()
    {
        using var sidecar = new TSidecar();
        string original = sidecar.TSourceCreate("original.mp4", "AAAA");
        string other = sidecar.TSourceCreate("other.mp4", "BBBB");
        Assert.True(sidecar.TSidecarSave(original, TimeSpan.FromSeconds(4), new long[] { 0, 1000 }));
        sidecar.TSidecarPersistCopy(original, other);

        Assert.Null(sidecar.TSidecarLoad(other, TimeSpan.FromSeconds(4)));
    }

    [Fact]
    public void ChangedSourceContent_InvalidatesCachedData()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("changing.mp4", "before");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(6), new long[] { 0, 1500 }));

        sidecar.TSidecarSourceSet(source, "after!");

        Assert.Null(sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(6)));
    }

    [Fact]
    public void ChangedSourceLength_ParticipatesInValidityCheck()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("resized.mp4", "short");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(3), new long[] { 0, 500 }));

        sidecar.TSidecarSourceSet(source, "a substantially longer source");

        Assert.Null(sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(3)));
    }
}
