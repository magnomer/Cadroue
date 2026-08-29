using Xunit;

namespace Cadroue.Tests;

public sealed class CheckupFormatTests
{
    [Fact]
    public void BodyFormat_Clean_ReturnsCleanString()
    {
        Assert.Equal(TCheckup.Clean, TCheckup.CleanFormat());
    }

    [Fact]
    public void BodyFormat_Failed_ReturnsFailedString()
    {
        Assert.Equal(TCheckup.Failed, TCheckup.FailedFormat());
    }

    [Fact]
    public void BodyFormat_Defect_ComposesLabelledLines()
    {
        string body = TCheckup.DefectFormat(
            "Container damage",
            "ffprobe -show_error reported a bad box size",
            "Rewrite the container");

        Assert.Equal(
            $"{TCheckup.DefectLabel}: Container damage\n"
            + $"{TCheckup.EvidenceLabel}: ffprobe -show_error reported a bad box size\n"
            + $"{TCheckup.RepairLabel}: Rewrite the container",
            body);
    }

    [Fact]
    public void BodyFormat_DefectOutcomeWithoutDossier_FallsBackToFailed()
    {
        Assert.Equal(TCheckup.Failed, TCheckup.DefectMissingDossierFormat());
    }
}
