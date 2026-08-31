using Xunit;

namespace Cadroue.Tests;

[Collection("Checkup")]
public sealed class TCheckupProgress
{
    [Fact]
    public void FfmpegTimestamp_MapsIntoCurrentDiagnosisStage()
    {
        var progress = new TCheckupProbe();

        TCheckup.TCheckupProgressApply(
            "out_time_us=5000000",
            TimeSpan.FromSeconds(10),
            0.25,
            0.5,
            progress);

        Assert.Equal(0.375, progress.TCheckupProbeRead(), 3);
    }

    [Fact]
    public void ScannerProgress_IsPublishedWithItsSourcePath()
    {
        using var checkup = new TCheckupJob((_, _, progress) => progress?.Report(0.42));

        checkup.TCheckupStart("progressing.mp4");

        Assert.True(SpinWait.SpinUntil(
            () => checkup.TCheckupProgressRead().Count > 0,
            TimeSpan.FromSeconds(5)));
        TCheckupSample report = Assert.Single(checkup.TCheckupProgressRead());
        Assert.Equal("progressing.mp4", report.TCheckupPath);
        Assert.Equal(0.42, report.TCheckupValue, 3);
    }

    private sealed class TCheckupProbe : IProgress<double>
    {
        private double tCheckupProbeValue;

        internal double TCheckupProbeRead() => tCheckupProbeValue;

        public void Report(double value) => tCheckupProbeValue = value;
    }
}
