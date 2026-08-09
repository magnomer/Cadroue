using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class SidecarPersistenceTests
{
    [Fact]
    public void Keyframes_RoundTripWithoutTimeChanges()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("keyframes.mp4", "keyframe source");
        long[] expected = { 0, 1001, 2500, 7999 };

        Assert.True(sidecar.Save(source, TimeSpan.FromSeconds(8), expected, new[] { 0, 2, 7 }));

        TSidecar.TSidecarData? loaded = sidecar.Load(source, TimeSpan.FromSeconds(8));
        Assert.NotNull(loaded);
        Assert.Equal(expected, loaded.Keyframes);
        Assert.Equal(new[] { 0, 2, 7 }, loaded.ScannedSpans);
    }

    [Fact]
    public void WaveformCache_RoundTripsPersistedValues()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("waveform.wav", "waveform source");
        Assert.True(sidecar.Save(source, TimeSpan.FromMilliseconds(4321), new long[] { 0 }));

        Assert.True(sidecar.WaveformSave(source, 25, 4321, "AQIDBA==", "BQYHCA=="));

        TSidecar.TSidecarData? loaded = sidecar.Load(source, TimeSpan.FromMilliseconds(4321));
        Assert.NotNull(loaded);
        Assert.Equal(new TSidecar.TSidecarWaveform(25, 4321, "AQIDBA==", "BQYHCA=="), loaded.Waveform);
    }

    [Fact]
    public void SavingChangedMetadata_ReplacesStaleValue()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("loudness.wav", "loudness source");

        Assert.True(sidecar.LoudnessSave(source, -23));
        Assert.True(sidecar.LoudnessSave(source, -14.25));

        Assert.Equal(-14.25, sidecar.Read(source)?.Loudness);
    }

    [Fact]
    public void MissingSidecar_LoadsAsCleanAbsence()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("missing.mp4", "no sidecar yet");

        Assert.Null(sidecar.Load(source, TimeSpan.FromSeconds(1)));
        Assert.Null(sidecar.Read(source));
    }
}
