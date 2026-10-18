using System.Text.Json.Nodes;

using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

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

        Assert.True(sidecar.TWaveformSave(source, 25, 4321, "AQIDBA=="));

        TSidecar.TSidecarData? loaded = sidecar.TSidecarLoad(source, TimeSpan.FromMilliseconds(4321));
        Assert.NotNull(loaded);
        Assert.Equal(new TSidecar.TSidecarWaveform(25, 4321, "AQIDBA=="), loaded.TSidecarWave);
    }

    [Fact]
    public void SavingChangedMetadata_ReplacesStaleValue()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("rotation.mp4", "rotation source");

        Assert.True(sidecar.TRotateSave(source, 90));
        Assert.True(sidecar.TRotateSave(source, 180));

        Assert.Equal(180, sidecar.TSidecarRead(source)?.TSidecarRotation);
    }

    [Fact]
    public void MissingSidecar_LoadsAsCleanAbsence()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("missing.mp4", "no sidecar yet");

        Assert.Null(sidecar.TSidecarLoad(source, TimeSpan.FromSeconds(1)));
        Assert.Null(sidecar.TSidecarRead(source));
    }

    [Fact]
    public void FileLocation_KeepsDistinctRecordsPerExtension()
    {
        using var sidecar = new TSidecar();
        sidecar.TSidecarLocationSet(true);
        string video = sidecar.TSourceCreate("clip.mp4", "video bytes");
        string container = sidecar.TSourceCreate("clip.mkv", "other bytes!");

        Assert.True(sidecar.TRotateSave(video, 90));
        Assert.True(sidecar.TRotateSave(container, 180));

        Assert.EndsWith("clip.mp4.cad", sidecar.TSidecarPathRead(video));
        Assert.EndsWith("clip.mkv.cad", sidecar.TSidecarPathRead(container));
        Assert.Equal(90, sidecar.TRotateRead(video));
        Assert.Equal(180, sidecar.TRotateRead(container));
    }

    [Fact]
    public void FileLocation_AdoptsLegacyRecordNamedForItsSource()
    {
        using var sidecar = new TSidecar();
        sidecar.TSidecarLocationSet(true);
        string video = sidecar.TSourceCreate("legacy.mp4", "legacy bytes");
        sidecar.TSidecarLegacySave(
            video,
            """
            {"LSidecarVersion":2,"LSidecarSource":{"LSidecarFileName":"legacy.mp4"},
            "LSidecarEdit":{"LSidecarRotation":270}}
            """);

        Assert.EndsWith("legacy.mp4.cad", sidecar.TSidecarPathRead(video));
        Assert.False(File.Exists(Path.ChangeExtension(video, ".cad")));
        Assert.Equal(270, sidecar.TSidecarRead(video)?.TSidecarRotation);
    }

    [Fact]
    public void CoreSave_PreservesMembersItDoesNotOwn()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("future.mp4", "future source");
        Assert.True(sidecar.TRotateSave(source, 90));

        JsonObject core = (JsonObject)JsonNode.Parse(sidecar.TSidecarTextRead(source))!;
        core["LSidecarFuture"] = new JsonObject { ["LSidecarValue"] = 42 };
        sidecar.TSidecarTextSave(source, core.ToJsonString());

        Assert.True(sidecar.TRotateSave(source, 180));

        JsonObject saved = (JsonObject)JsonNode.Parse(sidecar.TSidecarTextRead(source))!;
        Assert.Equal(42, (int)saved["LSidecarFuture"]!["LSidecarValue"]!);
        Assert.Equal(180, sidecar.TRotateRead(source));
    }

    [Fact]
    public void CoreSave_QuarantinesMalformedRecordInsteadOfSilentReplace()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("broken.mp4", "broken source");
        Assert.True(sidecar.TRotateSave(source, 90));
        sidecar.TSidecarTextSave(source, "{ not json");

        Assert.True(sidecar.TRotateSave(source, 270));

        Assert.Equal(1, sidecar.TSidecarBrokenRead());
        Assert.Equal(270, sidecar.TRotateRead(source));
    }

    [Fact]
    public void CoreSave_RefusesWhenSourceIsMissing()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("gone.mp4", "gone source");
        Assert.True(sidecar.TRotateSave(source, 90));
        File.Delete(source);

        Assert.False(sidecar.TRotateSave(source, 180));
    }
}
