using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlawTiming
{
    private static string TFlawPacketCreate(int stream, string pts, string dts) =>
        $"[PACKET]\nstream_index={stream}\npts={pts}\ndts={dts}\nduration=512\n[/PACKET]\n";

    [Fact]
    public void EmptyPacketReport_ProducesNoTimingDossier()
    {
        Assert.Null(TInterface.TFlawTimingResolve(string.Empty));
    }

    [Fact]
    public void MonotonicTimeline_ProducesNoTimingDossier()
    {
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "0", "0") + TFlawPacketCreate(0, "512", "512") + TFlawPacketCreate(0, "1024", "1024")));
    }

    [Fact]
    public void ReorderedPresentation_IsNotATimingDefect()
    {
        // B-frame reorder: PTS differs from DTS but DTS stays monotonic; legal.
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "1024", "0") + TFlawPacketCreate(0, "512", "512") + TFlawPacketCreate(0, "2048", "1024")));
    }

    [Fact]
    public void MissingPresentation_RegeneratesWithGenpts()
    {
        // A stream that presents some timestamps yet drops others has a reconstructable
        // gap; genpts fills it from decode order.
        LDossier? dossier = TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "0", "0") + TFlawPacketCreate(0, "N/A", "512") + TFlawPacketCreate(0, "1024", "1024"));

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryTimeline, dossier.Value.LDossierCategory);
        Assert.Equal(LDossierPreservation.LDossierPreservationPacket, dossier.Value.LDossierPreservation);
        Assert.Equal(LDossierValidation.LDossierValidationUntested, dossier.Value.LDossierValidation);
        Assert.Equal("-fflags +genpts", dossier.Value.LDossierRepairInput);
        Assert.Equal(string.Empty, dossier.Value.LDossierRepairArgument);
    }

    [Fact]
    public void UniformlyAbsentPresentation_IsContainerConvention()
    {
        // Every packet lacks a PTS (AVI without reordering): presentation order equals
        // decode order, a container convention rather than a defect to regenerate.
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "N/A", "0") + TFlawPacketCreate(0, "N/A", "512") + TFlawPacketCreate(0, "N/A", "1024")));
    }

    [Fact]
    public void StrayPresentationAmongAbsent_IsContainerConvention()
    {
        // One packet out of many carries a PTS while the rest do not: presentation timing is
        // not the stream's norm, so the lone stamp is a container artifact, not a fillable gap.
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "N/A", "0") + TFlawPacketCreate(0, "N/A", "512") + TFlawPacketCreate(0, "N/A", "1024")
            + TFlawPacketCreate(0, "N/A", "1536") + TFlawPacketCreate(0, "2048", "2048")));
    }

    [Fact]
    public void MissingDecode_IgnoresDtsWithIgndts()
    {
        LDossier? dossier = TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "0", "N/A") + TFlawPacketCreate(0, "512", "N/A"));

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryTimeline, dossier.Value.LDossierCategory);
        Assert.Equal("-fflags +igndts", dossier.Value.LDossierRepairInput);
    }

    [Fact]
    public void NonMonotonicDecode_IgnoresDtsWithIgndts()
    {
        LDossier? dossier = TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "0", "0") + TFlawPacketCreate(0, "512", "512") + TFlawPacketCreate(0, "256", "256"));

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryTimeline, dossier.Value.LDossierCategory);
        Assert.Equal("-fflags +igndts", dossier.Value.LDossierRepairInput);
    }

    [Fact]
    public void WraparoundDecode_IsNotATimingDefect()
    {
        // MPEG-TS 33-bit wraparound: DTS falls back from near 2^33 to zero; legal.
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "8589933000", "8589933000") + TFlawPacketCreate(0, "512", "512")));
    }

    [Fact]
    public void PerStreamOrdering_IgnoresCrossStreamInterleave()
    {
        // Two streams interleaved: each stream's own DTS is monotonic though the
        // report alternates between them. No defect.
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "0", "0") + TFlawPacketCreate(1, "0", "0")
            + TFlawPacketCreate(0, "512", "512") + TFlawPacketCreate(1, "512", "512")));
    }
}
