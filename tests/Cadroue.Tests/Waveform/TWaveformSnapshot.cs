using Xunit;

namespace Cadroue.Tests;

public sealed class TWaveformSnapshot
{
    [Fact]
    public void Peaks_DecodeToExpectedValues()
    {
        byte[] expected = { 0, 1, 127, 255 };
        TWaveformRecord record = TWaveform.TWaveformRecordCreate(expected, new byte[] { 1 }, TimeSpan.FromSeconds(1));

        Assert.Equal(expected, TWaveform.TWaveformPeaksRead(record));
    }

    [Fact]
    public void Rms_DecodeToExpectedValues()
    {
        byte[] expected = { 3, 32, 96, 192 };
        TWaveformRecord record = TWaveform.TWaveformRecordCreate(new byte[] { 1 }, expected, TimeSpan.FromSeconds(1));

        Assert.Equal(expected, TWaveform.TWaveformRmsRead(record));
    }

    [Fact]
    public void DurationWithinOneBucket_MatchesProductionValidityRule()
    {
        TimeSpan duration = TimeSpan.FromSeconds(3);
        TWaveformRecord record = TWaveform.TWaveformRecordCreate(new byte[] { 1 }, new byte[] { 1 }, duration);

        Assert.True(TWaveform.TWaveformRecordMatch(
            record,
            duration + TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds)));
        Assert.False(TWaveform.TWaveformRecordMatch(
            record,
            duration + TimeSpan.FromMilliseconds(TWaveform.TWaveformBucketMilliseconds + 1)));
    }

    [Fact]
    public void MissingOrMalformedEncodedData_DecodesAsEmpty()
    {
        var malformed = new TWaveformRecord(
            TWaveform.TWaveformBucketMilliseconds,
            1_000,
            "not base64",
            string.Empty);

        Assert.Empty(TWaveform.TWaveformPeaksRead(malformed));
        Assert.Empty(TWaveform.TWaveformRmsRead(malformed));
        Assert.Empty(TWaveform.TWaveformPeaksRead(null));
        Assert.False(TWaveform.TWaveformRecordMatch(malformed, TimeSpan.FromSeconds(1)));
    }
}
