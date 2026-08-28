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

    [Fact]
    public void ConsistentMetadata_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=10.000000\n[/STREAM]\n"
            + "[STREAM]\nindex=1\ncodec_name=aac\nduration=10.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=2\nduration=10.000000\n[/FORMAT]\n"));
    }

    [Fact]
    public void MissingDeclaredDuration_ProducesMetadataDossier()
    {
        LDossier? dossier = TInterface.FlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=12.500000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=1\nduration=N/A\n[/FORMAT]\n");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryMetadata, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("duration", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContradictoryDuration_ProducesMetadataDossier()
    {
        LDossier? dossier = TInterface.FlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=60.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=1\nduration=10.000000\n[/FORMAT]\n");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryMetadata, dossier.Value.LDossierCategory);
    }

    [Fact]
    public void DeclaredStreamCountMismatch_ProducesMetadataDossier()
    {
        LDossier? dossier = TInterface.FlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=10.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=3\nduration=10.000000\n[/FORMAT]\n");

        Assert.NotNull(dossier);
        Assert.Contains("stream count", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CleanTraversal_ProducesNoIndexDossier()
    {
        Assert.Null(TInterface.FlawIndexResolve(string.Empty, string.Empty, string.Empty));
    }

    [Fact]
    public void IndexDisagreement_ProducesIndexDossier()
    {
        LDossier? dossier = TInterface.FlawIndexResolve(
            "[mov,mp4 @ 0x1] Invalid sample offset in stts at pos: 51200",
            string.Empty,
            string.Empty);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryIndex, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("51200", dossier.Value.LDossierScope, System.StringComparison.Ordinal);
    }

    [Fact]
    public void SeekFailure_ProducesIndexDossier()
    {
        LDossier? dossier = TInterface.FlawIndexResolve(
            string.Empty,
            string.Empty,
            "[mov,mp4 @ 0x1] Could not seek to timestamp 27.000000");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryIndex, dossier.Value.LDossierCategory);
    }

    [Fact]
    public void UnreliableBoundaries_ProduceNoIndexDossier()
    {
        Assert.Null(TInterface.FlawIndexResolve(
            "[mov,mp4 @ 0x1] Invalid sample offset at pos: 51200",
            "[h264 @ 0x1] Invalid NAL unit size",
            string.Empty));
    }

    [Fact]
    public void MissingOptionalIndex_IsNotAnIndexDefect()
    {
        Assert.Null(TInterface.FlawIndexResolve(
            "[matroska @ 0x1] Could not find Cues element; seeking will be slow",
            string.Empty,
            string.Empty));
    }
}
