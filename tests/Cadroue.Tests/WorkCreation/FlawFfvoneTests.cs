using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawFfvoneTests
{
    private const string FfvoneStream =
        "[STREAM]\ncodec_type=video\ncodec_name=ffv1\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=matroska,webm\n[/FORMAT]\n";

    private const string H264Stream =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=matroska,webm\n[/FORMAT]\n";

    [Fact]
    public void NonFfvoneStream_ProducesNoFfvoneDossier()
    {
        Assert.Null(TInterface.FlawFfvoneResolve(
            H264Stream,
            "[ffv1 @ 0x1] CRC mismatch 1A2B3C4D!"));
    }

    [Fact]
    public void CleanFfvoneStream_ProducesNoFfvoneDossier()
    {
        Assert.Null(TInterface.FlawFfvoneResolve(FfvoneStream, string.Empty));
    }

    [Fact]
    public void FfvoneCrcMismatch_ProducesReportOnlyDossier()
    {
        LDossier? dossier = TInterface.FlawFfvoneResolve(
            FfvoneStream,
            "[ffv1 @ 0x1] CRC mismatch 1A2B3C4D!");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryReencode, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationExact, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal("report", dossier.Value.LDossierRepair);
        Assert.Equal(string.Empty, dossier.Value.LDossierRepairArgument);
        Assert.Equal(string.Empty, dossier.Value.LDossierRepairInput);
        Assert.Contains("CRC mismatch", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }
}
