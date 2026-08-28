using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawTests
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
            "[mov,mp4 @ 0x1] moov atom not found",
            "Invalid data found when processing input");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryContainer, dossier.Value.LDossierCategory);
        Assert.Equal(
            LDossierPreservation.LDossierPreservationPacket,
            dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("moov", dossier.Value.LDossierEvidenceSource, System.StringComparison.Ordinal);
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
}
