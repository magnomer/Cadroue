using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[Collection("Localization")]
public sealed class TInfoReport
{
    private static readonly LMediaInfo tInfoVideo = TInterface.TInfoVideoCreate(TimeSpan.FromSeconds(75), 640, 360);

    private static string[] TInfoKindsRead(IReadOnlyList<LInfoRow> rows) =>
        rows.Where(row => row.LInfoRowKind != LInfoRow.LInfoRowSeparator).Select(row => row.LInfoRowKind).ToArray();

    [Fact]
    public void NoCargo_ReadsOneMutedNotice()
    {
        LInfoRow row = Assert.Single(TInterface.TInfoReportRead(null, true));

        Assert.Equal(LInfoRow.LInfoRowMuted, row.LInfoRowKind);
        Assert.Equal(TInterface.TLocalizationTextRead("Source.Empty.Notice"), row.LInfoRowCaption);
    }

    [Theory]
    [InlineData(true, "Info.FFmpeg.Ready")]
    [InlineData(false, "Info.FFmpeg.Missing")]
    [InlineData(null, "Info.FFmpeg.Missing")]
    public void EmptyPath_ReadsGreyAvailability_FromThePassedValue(bool? availability, string key)
    {
        LCargo cargo = TInterface.TInfoCargoCreate(string.Empty, null, false, false, null);

        LInfoRow row = Assert.Single(TInterface.TInfoReportRead(cargo, availability));

        Assert.Equal(LInfoRow.LInfoRowGrey, row.LInfoRowKind);
        Assert.Equal(TInterface.TLocalizationTextRead(key), row.LInfoRowCaption);
    }

    [Fact]
    public void Video_SeparatesEveryRow_AndListsFacts()
    {
        LCargo cargo = TInterface.TInfoCargoCreate(@"C:\clip.mp4", tInfoVideo, true, false, null);

        IReadOnlyList<LInfoRow> rows = TInterface.TInfoReportRead(cargo, null);

        Assert.Equal(
            [LInfoRow.LInfoRowGood, LInfoRow.LInfoRowBad, LInfoRow.LInfoRowText, LInfoRow.LInfoRowText,
                LInfoRow.LInfoRowText, LInfoRow.LInfoRowText, LInfoRow.LInfoRowText],
            TInfoKindsRead(rows));
        Assert.Equal(13, rows.Count);
        Assert.All(
            rows.Where((_, index) => index % 2 == 1),
            row => Assert.Equal(LInfoRow.LInfoRowSeparator, row.LInfoRowKind));
        Assert.Equal("640×360", rows[6].LInfoRowCaption);
        Assert.Equal("25 fps", rows[8].LInfoRowCaption);
    }

    [Fact]
    public void MissingInfo_ReadsOnlyThePresentErrors()
    {
        LCargo cargo = TInterface.TInfoCargoCreate(@"C:\broken.mp4", null, false, false, "decoder");

        string[] kinds = TInfoKindsRead(TInterface.TInfoReportRead(cargo, null));

        Assert.Equal([LInfoRow.LInfoRowBad, LInfoRow.LInfoRowBad, LInfoRow.LInfoRowMuted], kinds);
    }
}
