using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlawMetadata
{
    [Fact]
    public void ConsistentMetadata_ProducesNoDossier()
    {
        Assert.Null(TInterface.TFlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=10.000000\n[/STREAM]\n"
            + "[STREAM]\nindex=1\ncodec_name=aac\nduration=10.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=2\nduration=10.000000\n[/FORMAT]\n"));
    }

    [Fact]
    public void MissingDeclaredDuration_ProducesMetadataDossier()
    {
        LDossier? dossier = TInterface.TFlawMetadataResolve(
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
        LDossier? dossier = TInterface.TFlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=60.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=1\nduration=10.000000\n[/FORMAT]\n");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryMetadata, dossier.Value.LDossierCategory);
    }

    [Fact]
    public void DisagreeingTrackTimelines_ProduceMetadataDossier()
    {
        // Video declares a 20s timeline over the same essence the 4s audio track spans,
        // and the format duration agrees with the longest track — the inflated per-track
        // timescale is still a metadata defect.
        LDossier? dossier = TInterface.TFlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=20.000000\n[/STREAM]\n"
            + "[STREAM]\nindex=1\ncodec_name=aac\nduration=4.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=2\nduration=20.000000\n[/FORMAT]\n");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryMetadata, dossier.Value.LDossierCategory);
        Assert.Contains("timelines disagree", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CloseTrackTimelines_ProduceNoMetadataDossier()
    {
        // Audio a hair longer than video is normal interleaving, not a defect.
        Assert.Null(TInterface.TFlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=10.000000\n[/STREAM]\n"
            + "[STREAM]\nindex=1\ncodec_name=aac\nduration=10.020000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=2\nduration=10.020000\n[/FORMAT]\n"));
    }

    [Fact]
    public void DeclaredStreamCountMismatch_ProducesMetadataDossier()
    {
        LDossier? dossier = TInterface.TFlawMetadataResolve(
            "[STREAM]\nindex=0\ncodec_name=h264\nduration=10.000000\n[/STREAM]\n"
            + "[FORMAT]\nnb_streams=3\nduration=10.000000\n[/FORMAT]\n");

        Assert.NotNull(dossier);
        Assert.Contains("stream count", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }
}
