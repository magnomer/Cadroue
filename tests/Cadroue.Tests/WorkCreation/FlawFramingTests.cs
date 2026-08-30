using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawFramingTests
{
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
}
