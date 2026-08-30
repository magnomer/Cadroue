using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class FlawSecondaryTests
{
    private const string SecondaryWithSubtitle =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[STREAM]\ncodec_type=subtitle\ncodec_name=subrip\n[/STREAM]\n";

    private const string SecondaryVideoOnly =
        "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
        + "[STREAM]\ncodec_type=audio\ncodec_name=aac\n[/STREAM]\n";

    [Fact]
    public void NoSecondaryObject_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawSecondaryResolve(SecondaryVideoOnly, string.Empty, string.Empty));
    }

    [Fact]
    public void CleanSecondaryStream_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawSecondaryResolve(SecondaryWithSubtitle, string.Empty, string.Empty));
    }

    [Fact]
    public void EmptyMuxerComplaint_IsNotASecondaryDefect()
    {
        // The secondary-only pass over a file with no subtitle/data output stream makes
        // the null muxer complain; that carries no secondary defect.
        Assert.Null(TInterface.FlawSecondaryResolve(
            SecondaryVideoOnly,
            string.Empty,
            "Output file #0 does not contain any stream"));
    }

    [Fact]
    public void MalformedSubtitleCarriage_ProducesSecondaryDossier()
    {
        LDossier? dossier = TInterface.FlawSecondaryResolve(
            SecondaryWithSubtitle,
            string.Empty,
            "[srt @ 0x1] Invalid data found when processing input");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategorySecondary, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Contains("Invalid data", dossier.Value.LDossierEvidenceSource, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ReversedChapter_ProducesSecondaryDossier()
    {
        LDossier? dossier = TInterface.FlawSecondaryResolve(
            SecondaryVideoOnly,
            "[CHAPTER]\nstart_time=10.000000\nend_time=5.000000\n[/CHAPTER]\n",
            string.Empty);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategorySecondary, dossier.Value.LDossierCategory);
        Assert.Contains("chapter", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OverlappingChapters_ProduceSecondaryDossier()
    {
        LDossier? dossier = TInterface.FlawSecondaryResolve(
            SecondaryVideoOnly,
            "[CHAPTER]\nstart_time=0.000000\nend_time=10.000000\n[/CHAPTER]\n"
            + "[CHAPTER]\nstart_time=5.000000\nend_time=15.000000\n[/CHAPTER]\n",
            string.Empty);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategorySecondary, dossier.Value.LDossierCategory);
    }

    [Fact]
    public void MonotonicChapters_ProduceNoDossier()
    {
        Assert.Null(TInterface.FlawSecondaryResolve(
            SecondaryVideoOnly,
            "[CHAPTER]\nstart_time=0.000000\nend_time=10.000000\n[/CHAPTER]\n"
            + "[CHAPTER]\nstart_time=10.000000\nend_time=20.000000\n[/CHAPTER]\n",
            string.Empty));
    }

    [Fact]
    public void AttachmentMissingFilename_ProducesSecondaryDossier()
    {
        LDossier? dossier = TInterface.FlawSecondaryResolve(
            "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
            + "[STREAM]\ncodec_type=attachment\ncodec_name=ttf\n[/STREAM]\n",
            string.Empty,
            string.Empty);

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategorySecondary, dossier.Value.LDossierCategory);
        Assert.Contains("filename", dossier.Value.LDossierEvidenceSource, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AttachmentWithFilename_ProducesNoDossier()
    {
        Assert.Null(TInterface.FlawSecondaryResolve(
            "[STREAM]\ncodec_type=video\ncodec_name=h264\n[/STREAM]\n"
            + "[STREAM]\ncodec_type=attachment\ncodec_name=ttf\nTAG:filename=font.ttf\n[/STREAM]\n",
            string.Empty,
            string.Empty));
    }
}
