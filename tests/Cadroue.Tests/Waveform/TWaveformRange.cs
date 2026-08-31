using Xunit;

namespace Cadroue.Tests;

public sealed class TWaveformRange
{
    [Fact]
    public void EmptyWaveform_ReturnsEmptyResult()
    {
        double[] result = TWaveform.TWaveformRangeRead(
            Array.Empty<byte>(),
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(40),
            1);

        Assert.Empty(result);
    }

    [Fact]
    public void SingleSample_CanBeReadWithoutIndexFailure()
    {
        double[] result = TWaveform.TWaveformRangeRead(
            new byte[] { 255 },
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds),
            1);

        Assert.Equal(new[] { 1d }, result);
    }

    [Fact]
    public void FullDuration_ReturnsEveryRelevantSample()
    {
        byte[] peaks = { 0, 64, 128, 255 };

        double[] result = TWaveform.TWaveformRangeRead(
            peaks,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(peaks.Length * TWaveform.TWaveformBucketMilliseconds),
            peaks.Length);

        Assert.Equal(peaks.Select(peak => peak / (double)TWaveform.TWaveformPeakMaximum), result);
    }

    [Fact]
    public void RangeStartingAtZero_SelectsInitialSamples()
    {
        double[] result = TWaveform.TWaveformRangeRead(
            new byte[] { 25, 75, 150, 225 },
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(2 * TWaveform.TWaveformBucketMilliseconds),
            2);

        Assert.Equal(new[] { 25 / 255d, 75 / 255d }, result);
    }

    [Fact]
    public void RangeEndingAtDuration_SelectsFinalSamples()
    {
        double[] result = TWaveform.TWaveformRangeRead(
            new byte[] { 25, 75, 150, 225 },
            TimeSpan.FromMilliseconds(2 * TWaveform.TWaveformBucketMilliseconds),
            TimeSpan.FromMilliseconds(4 * TWaveform.TWaveformBucketMilliseconds),
            2);

        Assert.Equal(new[] { 150 / 255d, 225 / 255d }, result);
    }

    [Fact]
    public void NarrowNonzeroRange_ReturnsRelevantMinimumSampleSet()
    {
        double[] result = TWaveform.TWaveformRangeRead(
            new byte[] { 25, 200, 75 },
            TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds + 1),
            TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds + 2),
            1);

        Assert.Equal(new[] { 200 / 255d }, result);
    }

    [Fact]
    public void WhollyOutOfRangeRequest_IsHandledSafely()
    {
        double[] result = TWaveform.TWaveformRangeRead(
            new byte[] { 25, 75 },
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(6),
            2);

        Assert.Equal(new[] { 0d, 0d }, result);
    }
}
