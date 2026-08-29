using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class SidecarDiagnosisTests
{
    [Fact]
    public void DiagnosisRead_ReturnsResult_OnUnchangedFile()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("diagnosis.mp4", "diagnosis source content");
        var dossiers = new[]
        {
            new TSidecar.TSidecarDossier("Container damage", "LFlawKindContainer")
        };
        Assert.True(sidecar.DiagnosisSave(source, TimeSpan.FromSeconds(3), dossiers));

        IReadOnlyList<TSidecar.TSidecarDossier>? read = sidecar.DiagnosisRead(source);

        Assert.NotNull(read);
        Assert.Single(read);
        Assert.Equal("Container damage", read[0].Defect);
        Assert.Equal("LFlawKindContainer", read[0].Kind);
    }

    [Fact]
    public void DiagnosisRead_ReturnsNull_AfterFileContentChanges()
    {
        using var sidecar = new TSidecar();
        string source = sidecar.SourceCreate("diagnosis-change.mp4", "original content");
        var dossiers = new[]
        {
            new TSidecar.TSidecarDossier("Truncated tail", "LFlawKindTruncation")
        };
        Assert.True(sidecar.DiagnosisSave(source, TimeSpan.FromSeconds(3), dossiers));
        Assert.NotNull(sidecar.DiagnosisRead(source));

        sidecar.SourceReplace(source, "original content grown to a different length and different bytes");

        Assert.Null(sidecar.DiagnosisRead(source));
    }
}
