using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TSidecarDiagnosis
{
    [Fact]
    public void DiagnosisRead_ReturnsResult_OnUnchangedFile()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("diagnosis.mp4", "diagnosis source content");
        var dossiers = new[]
        {
            new TSidecar.TSidecarDossier("Container damage", "LFlawKindContainer")
        };
        Assert.True(sidecar.TDiagnosisSave(source, TimeSpan.FromSeconds(3), dossiers));

        IReadOnlyList<TSidecar.TSidecarDossier>? read = sidecar.TDiagnosisRead(source);

        Assert.NotNull(read);
        Assert.Single(read);
        Assert.Equal("Container damage", read[0].TSidecarDefect);
        Assert.Equal("LFlawKindContainer", read[0].TSidecarKind);
    }

    [Fact]
    public void DiagnosisRead_ReturnsNull_AfterFileContentChanges()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("diagnosis-change.mp4", "original content");
        var dossiers = new[]
        {
            new TSidecar.TSidecarDossier("Truncated tail", "LFlawKindTruncation")
        };
        Assert.True(sidecar.TDiagnosisSave(source, TimeSpan.FromSeconds(3), dossiers));
        Assert.NotNull(sidecar.TDiagnosisRead(source));

        sidecar.TSidecarSourceSet(source, "original content grown to a different length and different bytes");

        Assert.Null(sidecar.TDiagnosisRead(source));
    }

    [Fact]
    public void DiagnosisRead_ReturnsNull_OnNeverDiagnosedFile()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("diagnosis-never.mp4", "never diagnosed content");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(3), new long[] { 0, 1_000 }));

        Assert.Null(sidecar.TDiagnosisRead(source));
    }

    [Fact]
    public void DiagnosisRead_ReturnsEmpty_OnCleanFile()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("diagnosis-clean.mp4", "clean content");
        Assert.True(sidecar.TDiagnosisSave(source, TimeSpan.FromSeconds(3), Array.Empty<TSidecar.TSidecarDossier>()));

        IReadOnlyList<TSidecar.TSidecarDossier>? read = sidecar.TDiagnosisRead(source);

        Assert.NotNull(read);
        Assert.Empty(read);
    }

    [Fact]
    public void DiagnosisRead_ReturnsNull_AfterKeyframeResaveOnChangedFile()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.TSourceCreate("diagnosis-restamp.mp4", "original content");
        var dossiers = new[]
        {
            new TSidecar.TSidecarDossier("Container damage", "LFlawKindContainer")
        };
        Assert.True(sidecar.TDiagnosisSave(source, TimeSpan.FromSeconds(3), dossiers));

        sidecar.TSidecarSourceSet(source, "original content grown to a different length and different bytes");
        Assert.True(sidecar.TSidecarSave(source, TimeSpan.FromSeconds(3), new long[] { 0, 1_000 }));

        Assert.Null(sidecar.TDiagnosisRead(source));
    }
}
