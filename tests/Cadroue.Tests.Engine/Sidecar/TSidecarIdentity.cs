using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

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

    [Fact]
    public void TypedRead_RefusesRecordOfReplacedSameLengthSource()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("replaced.mp4", "first!");
        Assert.True(sidecar.TRotateSave(source, 90));
        Assert.Equal(90, sidecar.TRotateRead(source));

        sidecar.TSidecarSourceSet(source, "second");

        Assert.Equal(0, sidecar.TRotateRead(source));
        Assert.True(sidecar.TRotateSave(source, 270));
        Assert.Equal(270, sidecar.TRotateRead(source));
    }

    [Fact]
    public void CachePayload_IsDroppedWhenCoreBelongsToNewerSource()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("regen.mp4", "first!");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(2), new long[] { 0, 700 }));
        Assert.True(sidecar.TWaveformSave(source, 25, 2000, "AQ=="));

        sidecar.TSidecarSourceSet(source, "second");
        Assert.True(sidecar.TRotateSave(source, 180));

        TSidecar.TSidecarData? read = sidecar.TSidecarRead(source);
        Assert.NotNull(read);
        Assert.Empty(read.TSidecarKeyframes);
        Assert.Null(read.TSidecarWave);
        Assert.Equal(180, read.TSidecarRotation);
    }

    [Fact]
    public void KeyframeLoad_RefusesRecordWhenWriteTimeDiffers()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("touched.mp4", "same bytes");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(2), new long[] { 0, 700 }));

        File.SetLastWriteTimeUtc(source, File.GetLastWriteTimeUtc(source).AddMinutes(5));

        Assert.Null(sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(2)));
    }
}
