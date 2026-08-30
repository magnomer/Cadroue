using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawContainerTests
{
    [Fact]
    public void CleanParse_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawContainerResolve(string.Empty, string.Empty));
    }

    [Fact]
    public void MissingOptionalIndex_IsNotAContainerDefect()
    {
        Assert.Null(TInterface.FlawContainerResolve(
            string.Empty,
            "[matroska @ 0x1] Could not find Cues element; seeking will be slow"));
        Assert.Null(TInterface.FlawContainerResolve(
            string.Empty,
            "[avi @ 0x1] Something went wrong during idx1 creation"));
    }

    [Fact]
    public void StructuralError_ProducesContainerDossier()
    {
        LDossier? dossier = TInterface.FlawContainerResolve(
            "[mov,mp4 @ 0x1] Invalid atom size in stco",
            "[mov,mp4 @ 0x1] Found duplicated atom; skipping");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryContainer, dossier.Value.LDossierCategory);
        Assert.Equal(
            LDossierPreservation.LDossierPreservationPacket,
            dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("atom", dossier.Value.LDossierEvidenceSource, System.StringComparison.Ordinal);
    }

    [Fact]
    public void DecodeDamage_IsNotReportedAsContainerDefect()
    {
        // A decoder complaint the container probe surfaces belongs to the coded detector,
        // not to container structure.
        Assert.Null(TInterface.FlawContainerResolve(
            "[mpeg2video @ 0x1] slice below image (55 >= 6)",
            "[mpeg2video @ 0x1] slice below image (55 >= 6)"));
    }

    [Fact]
    public void TruncationSymptom_IsNotReportedAsContainerDefect()
    {
        Assert.Null(TInterface.FlawContainerResolve(
            "[mov,mp4 @ 0x1] moov atom not found",
            "Invalid data found when processing input"));
    }

    [Fact]
    public void ByteOffset_IsCarriedIntoScope()
    {
        LDossier? dossier = TInterface.FlawContainerResolve(
            string.Empty,
            "[mpegts @ 0x1] Invalid element size at pos: 40928");

        Assert.NotNull(dossier);
        Assert.Contains("40928", dossier.Value.LDossierScope, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TransportFault_IsNotReportedAsContainerDefect()
    {
        Assert.Null(TInterface.FlawContainerResolve(
            string.Empty,
            "[mpegts @ 0x1] Continuity check failed for pid 256 expected 3 got 5"));
    }
}
