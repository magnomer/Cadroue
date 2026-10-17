using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterCompare
{
    [Fact]
    public void Compare_FlagsOnlyTheOutputWhenItDiffers()
    {
        QRosterCompareRow same = TInterfaceRoster.TRosterCompareCreate("a", "a");
        QRosterCompareRow other = TInterfaceRoster.TRosterCompareCreate("a", "b");

        Assert.Equal(QRosterDetail.QRosterLinePlain, same.QRosterCompareSource.QRosterLineKey);
        Assert.Equal(QRosterDetail.QRosterLinePlain, same.QRosterCompareOutput.QRosterLineKey);
        Assert.Equal(QRosterDetail.QRosterLinePlain, other.QRosterCompareSource.QRosterLineKey);
        Assert.Equal(QRosterDetail.QRosterLineChanged, other.QRosterCompareOutput.QRosterLineKey);
        Assert.Equal("b", other.QRosterCompareOutput.QRosterLineText);
    }

    [Fact]
    public void Compares_StartWithAHeadRow_AndReadMeasuringUntilTheSourceIsMeasured()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;

        QRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.Equal(7, detail.QRosterDetailCompares.Count);
        QRosterCompareRow head = detail.QRosterDetailCompares[0];
        Assert.Equal(QRosterDetail.QRosterLineHead, head.QRosterCompareSource.QRosterLineKey);
        Assert.Equal(QRosterDetail.QRosterLineHead, head.QRosterCompareOutput.QRosterLineKey);
        string measuring = detail.QRosterDetailCompares[1].QRosterCompareSource.QRosterLineText;

        work.LWorkSourceMeasured = true;
        QRosterDetail measured = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.NotEqual(measuring, measured.QRosterDetailCompares[1].QRosterCompareSource.QRosterLineText);
    }

    [Fact]
    public void Compares_ShowDimensionFpsAndDurationChanges()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;
        work.LWorkSourceMeasured = true;
        work.LWorkSourceMedia = Interface.TInterface.TWorkMediaCreate(3840, 2160, 60, 10_000, true);
        work.LWorkOutputMedia = Interface.TInterface.TWorkMediaCreate(1920, 1080, 30, 5_000, true);

        QRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.Equal("3840 x 2160", detail.QRosterDetailCompares[2].QRosterCompareSource.QRosterLineText);
        Assert.Equal("1920 x 1080", detail.QRosterDetailCompares[2].QRosterCompareOutput.QRosterLineText);
        Assert.Equal(
            QRosterDetail.QRosterLineChanged, detail.QRosterDetailCompares[2].QRosterCompareOutput.QRosterLineKey);
        Assert.Equal("60 fps", detail.QRosterDetailCompares[3].QRosterCompareSource.QRosterLineText);
        Assert.Equal("30 fps", detail.QRosterDetailCompares[3].QRosterCompareOutput.QRosterLineText);
        Assert.Equal("0:10", detail.QRosterDetailCompares[5].QRosterCompareSource.QRosterLineText);
        Assert.Equal("0:05", detail.QRosterDetailCompares[5].QRosterCompareOutput.QRosterLineText);
        Assert.Equal("SOURCE", detail.QRosterDetailCompares[6].QRosterCompareSource.QRosterLineText);
        Assert.Equal("OUTPUT", detail.QRosterDetailCompares[6].QRosterCompareOutput.QRosterLineText);
        Assert.False(detail.QRosterDetailSound);
    }

    [Fact]
    public void Sounds_AppearWithASampleRate_AndFlagCodecBitrateAndLoudnessChanges()
    {
        using var schedule = new TSchedule();
        TScheduleWork added = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(added);
        LWorkItem work = added.TWorkItem;
        work.LWorkSourceMeasured = true;
        work.LWorkSourceMedia = Interface.TInterface.TWorkMediaCreate(1920, 1080, 30, 5_000, true) with
        {
            LWorkMediaSamplerate = 48000,
            LWorkMediaCodec = "aac",
            LWorkMediaBitrate = 192_000,
            LWorkMediaLoudness = -23
        };
        work.LWorkOutputMedia = Interface.TInterface.TWorkMediaCreate(1920, 1080, 30, 5_000, true) with
        {
            LWorkMediaSamplerate = 44100,
            LWorkMediaCodec = "opus",
            LWorkMediaBitrate = 128_000,
            LWorkMediaLoudness = -16
        };

        QRosterDetail detail = TInterfaceRoster.TRosterDetailCreate(schedule.TScheduleRead(), work);
        Assert.True(detail.QRosterDetailSound);
        Assert.Equal(4, detail.QRosterDetailSounds.Count);
        Assert.Equal("AAC", detail.QRosterDetailSounds[0].QRosterCompareSource.QRosterLineText);
        Assert.Equal("OPUS", detail.QRosterDetailSounds[0].QRosterCompareOutput.QRosterLineText);
        Assert.Equal("192k", detail.QRosterDetailSounds[1].QRosterCompareSource.QRosterLineText);
        Assert.Equal("128k", detail.QRosterDetailSounds[1].QRosterCompareOutput.QRosterLineText);
        Assert.Equal("48000 Hz", detail.QRosterDetailSounds[2].QRosterCompareSource.QRosterLineText);
        Assert.Equal("-23 LUFS", detail.QRosterDetailSounds[3].QRosterCompareSource.QRosterLineText);
        Assert.Equal("-23 LUFS", detail.QRosterDetailSounds[3].QRosterCompareOutput.QRosterLineText);
        Assert.All(
            detail.QRosterDetailSounds.Take(3),
            row => Assert.Equal(QRosterDetail.QRosterLineChanged, row.QRosterCompareOutput.QRosterLineKey));
    }
}
