using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawTransportTests
{
    private const string TransportFormatTs =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=mpegts\n[/FORMAT]\n";

    private const string TransportFormatMp4 =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[FORMAT]\nformat_name=mov,mp4,m4a,3gp,3g2,mj2\n[/FORMAT]\n";

    [Fact]
    public void NonTransportInput_ProducesNoTransportDossier()
    {
        Assert.Null(TInterface.FlawTransportResolve(
            TransportFormatMp4,
            "[mpegts @ 0x1] Continuity check failed for pid 256 expected 3 got 5"));
    }

    [Fact]
    public void CleanTransportStream_ProducesNoTransportDossier()
    {
        Assert.Null(TInterface.FlawTransportResolve(TransportFormatTs, string.Empty));
    }

    [Fact]
    public void ContinuityFault_ProducesTransportDossier()
    {
        LDossier? dossier = TInterface.FlawTransportResolve(
            TransportFormatTs,
            "[mpegts @ 0x1] Continuity check failed for pid 256 expected 3 got 5");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryTransport, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("Continuity", dossier.Value.LDossierEvidenceSource, System.StringComparison.Ordinal);
    }

    [Fact]
    public void DecodeDamageOnTransport_IsNotATransportDefect()
    {
        Assert.Null(TInterface.FlawTransportResolve(
            TransportFormatTs,
            "[h264 @ 0x1] error while decoding MB 12 34, bytestream -5"));
    }
}
