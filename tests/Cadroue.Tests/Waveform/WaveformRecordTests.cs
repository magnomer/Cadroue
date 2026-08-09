using Xunit;

namespace Cadroue.Tests;

public sealed class WaveformRecordTests
{
    [Fact]
    public void Peaks_DecodeToExpectedValues()
    {
        byte[] expected = { 0, 1, 127, 255 };
        TWaveformRecord record = TWaveform.RecordCreate(expected, new byte[] { 1 }, TimeSpan.FromSeconds(1));

        Assert.Equal(expected, TWaveform.PeaksRead(record));
    }

    [Fact]
    public void Rms_DecodeToExpectedValues()
    {
        byte[] expected = { 3, 32, 96, 192 };
        TWaveformRecord record = TWaveform.RecordCreate(new byte[] { 1 }, expected, TimeSpan.FromSeconds(1));

        Assert.Equal(expected, TWaveform.RmsRead(record));
    }

    [Fact]
    public void DurationWithinOneBucket_MatchesProductionValidityRule()
    {
        TimeSpan duration = TimeSpan.FromSeconds(3);
        TWaveformRecord record = TWaveform.RecordCreate(new byte[] { 1 }, new byte[] { 1 }, duration);

        Assert.True(TWaveform.RecordMatch(
            record,
            duration + TimeSpan.FromMilliseconds(TWaveform.BucketMilliseconds)));
        Assert.False(TWaveform.RecordMatch(
            record,
            duration + TimeSpan.FromMilliseconds(TWaveform.BucketMilliseconds + 1)));
    }

    [Fact]
    public void MissingOrMalformedEncodedData_DecodesAsEmpty()
    {
        var malformed = new TWaveformRecord(
            TWaveform.BucketMilliseconds,
            1_000,
            "not base64",
            string.Empty);

        Assert.Empty(TWaveform.PeaksRead(malformed));
        Assert.Empty(TWaveform.RmsRead(malformed));
        Assert.Empty(TWaveform.PeaksRead(null));
        Assert.False(TWaveform.RecordMatch(malformed, TimeSpan.FromSeconds(1)));
    }
}
