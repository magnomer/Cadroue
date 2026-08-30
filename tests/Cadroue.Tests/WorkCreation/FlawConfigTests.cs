using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawConfigTests
{
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
