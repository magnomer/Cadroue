using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlawFraming
{
    private const string TFlawFramingIso =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=mov,mp4,m4a,3gp,3g2,mj2\n[/FORMAT]\n";

    private const string TFlawFramingTs =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=mpegts\n[/FORMAT]\n";

    [Fact]
    public void CleanCopy_ProducesNoFramingDossier()
    {
        Assert.Null(TInterface.TFlawFramingResolve(string.Empty, TFlawFramingIso));
    }

    [Fact]
    public void NalFramingFault_InIsoContainer_ExtractsExtradata()
    {
        LDossier? dossier = TInterface.TFlawFramingResolve(
            "[h264 @ 0x1] Invalid NAL unit size (-1 > 123).", TFlawFramingIso);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryPacket, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationCoded, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal("-bsf:v extract_extradata", dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void NalFramingFault_InTransportStream_UsesMp4ToAnnexb()
    {
        LDossier? dossier = TInterface.TFlawFramingResolve(
            "[mpegts @ 0x1] Invalid NAL unit size (-1 > 123).", TFlawFramingTs);

        Assert.NotNull(dossier);
        Assert.Equal("-bsf:v h264_mp4toannexb", dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void DamagedPayload_IsNotAFramingDefect()
    {
        Assert.Null(TInterface.TFlawFramingResolve(
            "[h264 @ 0x1] error while decoding MB 12 34, bytestream -5", TFlawFramingIso));
    }

    [Fact]
    public void FramingFault_WithoutBitstreamFilterCodec_ProducesNoDossier()
    {
        Assert.Null(TInterface.TFlawFramingResolve(
            "[vp9 @ 0x1] Invalid NAL unit size (-1 > 123).",
            "[STREAM]\ncodec_type=video\ncodec_name=vp9\n[/STREAM]\n"
            + "[FORMAT]\nformat_name=matroska,webm\n[/FORMAT]\n"));
    }
}
