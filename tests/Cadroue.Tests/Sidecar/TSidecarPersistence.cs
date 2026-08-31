using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TSidecarPersistence
{
    [Fact]
    public void Keyframes_RoundTripWithoutTimeChanges()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("keyframes.mp4", "keyframe source");
        long[] expected = { 0, 1001, 2500, 7999 };

        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(8), expected, new[] { 0, 2, 7 }));

        TSidecar.TSidecarData? loaded = sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(8));
        Assert.NotNull(loaded);
        Assert.Equal(expected, loaded.TSidecarKeyframes);
        Assert.Equal(new[] { 0, 2, 7 }, loaded.TSidecarScannedSpans);
    }

    [Fact]
    public void WaveformCache_RoundTripsPersistedValues()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("waveform.wav", "waveform source");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromMilliseconds(4321), new long[] { 0 }));

        Assert.True(sidecar.TWaveformSave(source, 25, 4321, "AQIDBA==", "BQYHCA=="));

        TSidecar.TSidecarData? loaded = sidecar.TSidecarLoad(source, TimeSpan.FromMilliseconds(4321));
        Assert.NotNull(loaded);
        Assert.Equal(new TSidecar.TSidecarWaveform(25, 4321, "AQIDBA==", "BQYHCA=="), loaded.TSidecarWave);
    }

    [Fact]
    public void SavingChangedMetadata_ReplacesStaleValue()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("loudness.wav", "loudness source");

        Assert.True(sidecar.TLoudnessSave(source, -23));
        Assert.True(sidecar.TLoudnessSave(source, -14.25));

        Assert.Equal(-14.25, sidecar.TSidecarRead(source)?.TSidecarLoudness);
    }

    [Fact]
    public void MissingSidecar_LoadsAsCleanAbsence()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("missing.mp4", "no sidecar yet");

        Assert.Null(sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(1)));
        Assert.Null(sidecar.TSidecarRead(source));
    }
}
