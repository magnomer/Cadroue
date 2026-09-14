using Xunit;

namespace Cadroue.Tests;

public sealed class TWaveformSnapshot
{
    [Fact]
    public void Peaks_DecodeToExpectedValues()
    {
        byte[] expected = { 0, 1, 127, 255 };
        TWaveformRecord record = TWaveform.TWaveformRecordCreate(expected, TimeSpan.FromSeconds(1));

        Assert.Equal(expected, TWaveform.TWaveformPeaksRead(record));
    }

    [Fact]
    public void DurationWithinOneBucket_MatchesProductionValidityRule()
    {
        TimeSpan duration = TimeSpan.FromSeconds(3);
        byte[] peaks = new byte[TWaveform.TWaveformBucketsResolve(duration)];
        peaks[0] = 1;
        TWaveformRecord record = TWaveform.TWaveformRecordCreate(peaks, duration);

        Assert.True(TWaveform.TWaveformRecordMatch(
            record,
            duration + TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds)));
        Assert.False(TWaveform.TWaveformRecordMatch(
            record,
            duration + TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds + 1)));
    }

    [Fact]
    public void PeakCountOffTheDuration_FailsMatch()
    {
        TimeSpan duration = TimeSpan.FromSeconds(3);
        byte[] truncated = new byte[TWaveform.TWaveformBucketsResolve(duration) - 1];
        truncated[0] = 1;
        TWaveformRecord record = TWaveform.TWaveformRecordCreate(truncated, duration);

        Assert.False(TWaveform.TWaveformRecordMatch(record, duration));
    }

    [Fact]
    public void MissingOrMalformedEncodedData_DecodesAsEmpty()
    {
        var malformed = new TWaveformRecord(
            TWaveform.TWaveformBucketMilliseconds,
            1_000,
            "not base64");

        Assert.Empty(TWaveform.TWaveformPeaksRead(malformed));
        Assert.Empty(TWaveform.TWaveformPeaksRead(null));
        Assert.False(TWaveform.TWaveformRecordMatch(malformed, TimeSpan.FromSeconds(1)));
    }
}
