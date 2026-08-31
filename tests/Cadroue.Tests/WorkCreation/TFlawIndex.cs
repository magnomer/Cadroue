using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlawIndex
{
    [Fact]
    public void CleanTraversal_ProducesNoIndexDossier()
    {
        Assert.Null(TInterface.TFlawIndexResolve(string.Empty, string.Empty, string.Empty));
    }

    [Fact]
    public void IndexDisagreement_ProducesIndexDossier()
    {
        LDossier? dossier = TInterface.TFlawIndexResolve(
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
        LDossier? dossier = TInterface.TFlawIndexResolve(
            string.Empty,
            string.Empty,
            "[mov,mp4 @ 0x1] Could not seek to timestamp 27.000000");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryIndex, dossier.Value.LDossierCategory);
    }

    [Fact]
    public void UnreliableBoundaries_ProduceNoIndexDossier()
    {
        Assert.Null(TInterface.TFlawIndexResolve(
            "[mov,mp4 @ 0x1] Invalid sample offset at pos: 51200",
            "[h264 @ 0x1] Invalid NAL unit size",
            string.Empty));
    }

    [Fact]
    public void MissingOptionalIndex_IsNotAnIndexDefect()
    {
        Assert.Null(TInterface.TFlawIndexResolve(
            "[matroska @ 0x1] Could not find Cues element; seeking will be slow",
            string.Empty,
            string.Empty));
    }

    [Fact]
    public void SeekOnlyFailureOverCleanRead_ProducesIndexDossier()
    {
        // Sequential read is clean with and without the index, yet a boundary seek
        // fails with a message that never says "index": the addressing is broken.
        LDossier? dossier = TInterface.TFlawIndexResolve(
            string.Empty,
            string.Empty,
            "[matroska,webm @ 0x1] Length 8 indicated by an EBML number's first byte 0x01 "
            + "at pos 33763 exceeds max length 4.");

        Assert.NotNull(dossier);
        Assert.Equal(LDossierCategory.LDossierCategoryIndex, dossier.Value.LDossierCategory);
    }

    [Fact]
    public void SeekFailureOverDamagedRead_IsNotAnIndexDefect()
    {
        // The sequential read already errors, so the fault belongs to the container or
        // coded detector that owns that error, not to addressing.
        Assert.Null(TInterface.TFlawIndexResolve(
            "[matroska @ 0x1] 0x00 at pos 536 invalid as first byte of an EBML number",
            string.Empty,
            "[matroska @ 0x1] 0x00 at pos 536 invalid as first byte of an EBML number"));
    }
}
