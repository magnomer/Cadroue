using Xunit;

namespace Cadroue.Tests;

public sealed class TCheckupFormat
{
    [Fact]
    public void BodyFormat_Clean_ReturnsCleanString()
    {
        Assert.Equal(TCheckup.TCheckupClean, TCheckup.TCheckupCleanFormat());
    }

    [Fact]
    public void BodyFormat_Failed_ReturnsFailedString()
    {
        Assert.Equal(TCheckup.TCheckupFailed, TCheckup.TCheckupFailedFormat());
    }

    [Fact]
    public void BodyFormat_Defect_ComposesLabelledLines()
    {
        string body = TCheckup.TCheckupDefectFormat(
            "Container damage",
            "ffprobe -show_error reported a bad box size",
            "Rewrite the container");

        Assert.Equal(
            $"{TCheckup.TCheckupDefectLabel}: Container damage\n"
            + $"{TCheckup.TCheckupEvidenceLabel}: ffprobe -show_error reported a bad box size\n"
            + $"{TCheckup.TCheckupRepairLabel}: Rewrite the container",
            body);
    }

    [Fact]
    public void BodyFormat_DefectOutcomeWithoutDossier_FallsBackToFailed()
    {
        Assert.Equal(TCheckup.TCheckupFailed, TCheckup.TCheckupMissingFormat());
    }
}
