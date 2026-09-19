using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRosterCompare
{
    [Fact]
    public void Compare_FlagsOnlyTheOutputWhenItDiffers()
    {
        LRosterCompareRow same = TInterface.TRosterCompareCreate("a", "a");
        LRosterCompareRow other = TInterface.TRosterCompareCreate("a", "b");

        Assert.Equal(LRosterDetail.LRosterLinePlain, same.LRosterCompareSource.LRosterLineKey);
        Assert.Equal(LRosterDetail.LRosterLinePlain, same.LRosterCompareOutput.LRosterLineKey);
        Assert.Equal(LRosterDetail.LRosterLinePlain, other.LRosterCompareSource.LRosterLineKey);
        Assert.Equal(LRosterDetail.LRosterLineChanged, other.LRosterCompareOutput.LRosterLineKey);
        Assert.Equal("b", other.LRosterCompareOutput.LRosterLineText);
    }

    [Fact]
    public void Compares_StartWithAHeadRow_AndReadMeasuringUntilTheSourceIsMeasured()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;

        LRosterDetail detail = TInterface.TRosterDetailCreate(work, "-");
        Assert.Equal(7, detail.LRosterDetailCompares.Count);
        LRosterCompareRow head = detail.LRosterDetailCompares[0];
        Assert.Equal(LRosterDetail.LRosterLineHead, head.LRosterCompareSource.LRosterLineKey);
        Assert.Equal(LRosterDetail.LRosterLineHead, head.LRosterCompareOutput.LRosterLineKey);
        string measuring = detail.LRosterDetailCompares[1].LRosterCompareSource.LRosterLineText;

        work.LWorkSourceMeasured = true;
        LRosterDetail measured = TInterface.TRosterDetailCreate(work, "-");
        Assert.NotEqual(measuring, measured.LRosterDetailCompares[1].LRosterCompareSource.LRosterLineText);
    }

    [Fact]
    public void Compares_ShowDimensionFpsAndDurationChanges()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;
        work.LWorkSourceMeasured = true;
        work.LWorkSourceMedia = TInterface.TWorkMediaCreate(3840, 2160, 60, 10_000, true);
        work.LWorkOutputMedia = TInterface.TWorkMediaCreate(1920, 1080, 30, 5_000, true);

        LRosterDetail detail = TInterface.TRosterDetailCreate(work, "-");
        Assert.Equal("3840 x 2160", detail.LRosterDetailCompares[2].LRosterCompareSource.LRosterLineText);
        Assert.Equal("1920 x 1080", detail.LRosterDetailCompares[2].LRosterCompareOutput.LRosterLineText);
        Assert.Equal(
            LRosterDetail.LRosterLineChanged, detail.LRosterDetailCompares[2].LRosterCompareOutput.LRosterLineKey);
        Assert.Equal("60 fps", detail.LRosterDetailCompares[3].LRosterCompareSource.LRosterLineText);
        Assert.Equal("30 fps", detail.LRosterDetailCompares[3].LRosterCompareOutput.LRosterLineText);
        Assert.Equal("0:10", detail.LRosterDetailCompares[5].LRosterCompareSource.LRosterLineText);
        Assert.Equal("0:05", detail.LRosterDetailCompares[5].LRosterCompareOutput.LRosterLineText);
        Assert.Equal("SOURCE", detail.LRosterDetailCompares[6].LRosterCompareSource.LRosterLineText);
        Assert.Equal("OUTPUT", detail.LRosterDetailCompares[6].LRosterCompareOutput.LRosterLineText);
        Assert.False(detail.LRosterDetailSound);
    }

    [Fact]
    public void Sounds_AppearWithASampleRate_AndFlagCodecBitrateAndLoudnessChanges()
    {
        using var schedule = new TSchedule();
        LWorkItem work = schedule.TWorkCreate(Guid.NewGuid(), "one").TWorkItem;
        work.LWorkSourceMeasured = true;
        work.LWorkSourceMedia = TInterface.TWorkMediaCreate(1920, 1080, 30, 5_000, true) with
        {
            LWorkMediaSamplerate = 48000,
            LWorkMediaCodec = "aac",
            LWorkMediaBitrate = 192_000,
            LWorkMediaLoudness = -23
        };
        work.LWorkOutputMedia = TInterface.TWorkMediaCreate(1920, 1080, 30, 5_000, true) with
        {
            LWorkMediaSamplerate = 44100,
            LWorkMediaCodec = "opus",
            LWorkMediaBitrate = 128_000,
            LWorkMediaLoudness = -16
        };

        LRosterDetail detail = TInterface.TRosterDetailCreate(work, "-");
        Assert.True(detail.LRosterDetailSound);
        Assert.Equal(4, detail.LRosterDetailSounds.Count);
        Assert.Equal("AAC", detail.LRosterDetailSounds[0].LRosterCompareSource.LRosterLineText);
        Assert.Equal("OPUS", detail.LRosterDetailSounds[0].LRosterCompareOutput.LRosterLineText);
        Assert.Equal("192k", detail.LRosterDetailSounds[1].LRosterCompareSource.LRosterLineText);
        Assert.Equal("128k", detail.LRosterDetailSounds[1].LRosterCompareOutput.LRosterLineText);
        Assert.Equal("48000 Hz", detail.LRosterDetailSounds[2].LRosterCompareSource.LRosterLineText);
        Assert.Equal("-23 LUFS", detail.LRosterDetailSounds[3].LRosterCompareSource.LRosterLineText);
        Assert.Equal("-23 LUFS", detail.LRosterDetailSounds[3].LRosterCompareOutput.LRosterLineText);
        Assert.All(
            detail.LRosterDetailSounds.Take(3),
            row => Assert.Equal(LRosterDetail.LRosterLineChanged, row.LRosterCompareOutput.LRosterLineKey));
    }
}
