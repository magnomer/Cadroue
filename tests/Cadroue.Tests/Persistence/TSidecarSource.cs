using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TSidecarSource
{
    [Fact]
    public void Resolve_FindsVerifiedSourceBesideWrittenSidecar()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("clip.mp4", "media bytes");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(2), [0L, 1000L]));

        LSidecarSourceResult? result = sidecar.TSourceResolve(source);

        Assert.NotNull(result);
        Assert.True(result!.LSidecarResultVerified);
        Assert.Equal(Path.GetFullPath(source), result.LSidecarResultPath);
        Assert.Equal("clip.mp4", result.LSidecarResultName);
    }

    [Fact]
    public void Resolve_WithoutSidecar_IsNull()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("orphan.mp4", "media bytes");

        Assert.Null(sidecar.TSourceResolve(source));
    }

    [Fact]
    public void Match_AcceptsWrittenSource_RejectsChangedOne()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("clip.mp4", "media bytes");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(2), [0L]));

        Assert.True(sidecar.TSourceMatch(source, source));

        string other = sidecar.TSourceCreate("other.mp4", "different media bytes");
        Assert.False(sidecar.TSourceMatch(other, source));
    }
}
