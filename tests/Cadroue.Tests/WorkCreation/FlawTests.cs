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

    private const string FramingH264Mp4 =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=mov,mp4,m4a,3gp,3g2,mj2\n[/FORMAT]\n";

    private const string FramingH264Ts =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=mpegts\n[/FORMAT]\n";

    [Fact]
    public void CleanCopy_ProducesNoFramingDossier()
    {
        Assert.Null(TInterface.FlawFramingResolve(string.Empty, FramingH264Mp4));
    }

    [Fact]
    public void NalFramingFault_InIsoContainer_ExtractsExtradata()
    {
        LDossier? dossier = TInterface.FlawFramingResolve(
            "[h264 @ 0x1] Invalid NAL unit size (-1 > 123).", FramingH264Mp4);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryPacket, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationCoded, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal("-bsf:v extract_extradata", dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void NalFramingFault_InTransportStream_UsesMp4ToAnnexb()
    {
        LDossier? dossier = TInterface.FlawFramingResolve(
            "[mpegts @ 0x1] Invalid NAL unit size (-1 > 123).", FramingH264Ts);

        Assert.NotNull(dossier);
        Assert.Equal("-bsf:v h264_mp4toannexb", dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void DamagedPayload_IsNotAFramingDefect()
    {
        Assert.Null(TInterface.FlawFramingResolve(
            "[h264 @ 0x1] error while decoding MB 12 34, bytestream -5", FramingH264Mp4));
    }

    [Fact]
    public void FramingFault_WithoutBitstreamFilterCodec_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawFramingResolve(
            "[vp9 @ 0x1] Invalid NAL unit size (-1 > 123).",
            "[STREAM]\ncodec_type=video\ncodec_name=vp9\n[/STREAM]\n"
            + "[FORMAT]\nformat_name=matroska,webm\n[/FORMAT]\n"));
    }

    private const string ConfigH264NoExtradata =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\nextradata_size=0\n[/STREAM]\n";

    private const string ConfigH264WithExtradata =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\nextradata_size=48\n[/STREAM]\n";

    [Fact]
    public void CleanDecode_ProducesNoConfigDossier()
    {
        Assert.Null(TInterface.FlawConfigResolve(ConfigH264NoExtradata, string.Empty));
    }

    [Fact]
    public void MissingParameterSets_NoExtradata_ExtractsExtradata()
    {
        LDossier? dossier = TInterface.FlawConfigResolve(
            ConfigH264NoExtradata,
            "[h264 @ 0x1] non-existing PPS 0 referenced");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryConfig, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal("-bsf:v extract_extradata", dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void InconsistentExtradata_ReinsertsWithDumpExtra()
    {
        LDossier? dossier = TInterface.FlawConfigResolve(
            ConfigH264WithExtradata,
            "[h264 @ 0x1] SPS unavailable in decoding");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryConfig, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationCoded, dossier.Value.LDossierPreservation);
        Assert.Equal("-bsf:v dump_extra", dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void DamagedSlice_IsNotAConfigDefect()
    {
        Assert.Null(TInterface.FlawConfigResolve(
            ConfigH264NoExtradata,
            "[h264 @ 0x1] error while decoding MB 12 34, bytestream -5"));
    }

    [Fact]
    public void ConfigFault_WithoutParameterSetCodec_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawConfigResolve(
            "[STREAM]\ncodec_type=video\ncodec_name=vp9\nextradata_size=0\n[/STREAM]\n",
            "[vp9 @ 0x1] non-existing PPS 0 referenced"));
    }
}
