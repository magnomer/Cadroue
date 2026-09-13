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
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "1024", "0") + TFlawPacketCreate(0, "512", "512") + TFlawPacketCreate(0, "2048", "1024")));
    }

    [Fact]
    public void MissingPresentation_RegeneratesWithGenpts()
    {
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
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "N/A", "0") + TFlawPacketCreate(0, "N/A", "512") + TFlawPacketCreate(0, "N/A", "1024")));
    }

    [Fact]
    public void StrayPresentationAmongAbsent_IsContainerConvention()
    {
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
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "8589933000", "8589933000") + TFlawPacketCreate(0, "512", "512")));
    }

    [Fact]
    public void PerStreamOrdering_IgnoresCrossStreamInterleave()
    {
        Assert.Null(TInterface.TFlawTimingResolve(
            TFlawPacketCreate(0, "0", "0") + TFlawPacketCreate(1, "0", "0")
            + TFlawPacketCreate(0, "512", "512") + TFlawPacketCreate(1, "512", "512")));
    }
}
