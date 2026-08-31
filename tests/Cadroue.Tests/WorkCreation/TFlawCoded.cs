using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlawCoded
{
    [Fact]
    public void CleanDecode_ProducesNoCodedDossier()
    {
        Assert.Null(TInterface.TFlawCodedResolve(string.Empty));
    }

    [Fact]
    public void FramingFault_IsNotCodedDamage()
    {
        // A framing/config fault is repaired without decode; it is not decode damage
        // and must not be escalated to the last-resort re-encode item.
        Assert.Null(TInterface.TFlawCodedResolve("[h264 @ 0x1] Invalid NAL unit size (-1 > 123)."));
        Assert.Null(TInterface.TFlawCodedResolve("[h264 @ 0x1] non-existing PPS 0 referenced"));
    }

    [Fact]
    public void DecodeDamage_ProducesCodedDossier()
    {
        LDossier? dossier = TInterface.TFlawCodedResolve(
            "[h264 @ 0x1] error while decoding MB 12 34, bytestream -5");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryReencode, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationLossy, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal(
            "-err_detect ignore_err -fflags +discardcorrupt+genpts",
            dossier.Value.LDossierRepairInput);
        Assert.Contains("decoding", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConcealedFrame_ProducesCodedDossier()
    {
        LDossier? dossier = TInterface.TFlawCodedResolve("[hevc @ 0x1] concealing 512 DC, 512 AC errors");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryReencode, dossier.Value.LDossierCategory);
    }
}
