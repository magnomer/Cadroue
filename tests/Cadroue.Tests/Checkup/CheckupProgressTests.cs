using Xunit;

namespace Cadroue.Tests;

[Collection("Checkup")]
public sealed class CheckupProgressTests
{
    [Fact]
    public void FfmpegTimestamp_MapsIntoCurrentDiagnosisStage()
    {
        var progress = new TProgress();

        TCheckup.ProgressApply(
            "out_time_us=5000000",
            TimeSpan.FromSeconds(10),
            0.25,
            0.5,
            progress);

        Assert.Equal(0.375, progress.Value, 3);
    }

    [Fact]
    public void ScannerProgress_IsPublishedWithItsSourcePath()
    {
        using var checkup = new TCheckupJob((_, _, progress) => progress?.Report(0.42));

        checkup.Start("progressing.mp4");

        Assert.True(SpinWait.SpinUntil(
            () => checkup.ProgressRead().Count > 0,
            TimeSpan.FromSeconds(5)));
        TCheckupProgress report = Assert.Single(checkup.ProgressRead());
        Assert.Equal("progressing.mp4", report.Path);
        Assert.Equal(0.42, report.Value, 3);
    }

    private sealed class TProgress : IProgress<double>
    {
        public double Value { get; private set; }

        public void Report(double value) => Value = value;
    }
}
