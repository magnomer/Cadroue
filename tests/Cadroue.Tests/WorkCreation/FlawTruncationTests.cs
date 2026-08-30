using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawTruncationTests
{
    [Fact]
    public void CleanFile_ProducesNoTruncationDossier()
    {
        Assert.Null(TInterface.FlawTruncationResolve(string.Empty, string.Empty));
    }

    [Fact]
    public void MissingFinalMetadata_RebuildsFinalization()
    {
        LDossier? dossier = TInterface.FlawTruncationResolve(
            "[mov,mp4 @ 0x1] moov atom not found",
            string.Empty);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryTruncation, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal("None", dossier.Value.LDossierLoss);
        Assert.Contains("moov", dossier.Value.LDossierEvidenceSource, System.StringComparison.Ordinal);
    }

    [Fact]
    public void PhysicallyMissingTail_SalvagesPrefixWithConfirmedLoss()
    {
        LDossier? dossier = TInterface.FlawTruncationResolve(
            string.Empty,
            "[matroska @ 0x1] File ended prematurely; truncated at pos: 81920");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryTruncation, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationLossy, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("Confirmed", dossier.Value.LDossierLoss, System.StringComparison.Ordinal);
        Assert.Contains("81920", dossier.Value.LDossierScope, System.StringComparison.Ordinal);
    }

    [Fact]
    public void PhysicalTail_TakesPrecedenceOverMissingFinalMetadata()
    {
        LDossier? dossier = TInterface.FlawTruncationResolve(
            "[mov,mp4 @ 0x1] moov atom not found",
            "Invalid data found when processing input");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierPreservation.LDossierPreservationLossy, dossier.Value.LDossierPreservation);
        Assert.Contains("Confirmed", dossier.Value.LDossierLoss, System.StringComparison.Ordinal);
    }
}
