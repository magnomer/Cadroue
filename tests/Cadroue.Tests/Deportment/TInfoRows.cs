using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInfoRows
{
    private static (LViewer, LInfo) TInfoBuild()
    {
        LViewer viewer = TInterface.TViewerCreate();
        return (viewer, TInterface.TInfoCreate(viewer));
    }

    private static string[] TInfoKindsRead(LInfo info) =>
        TInterface.TInfoRowsRead(info)
            .Where(row => row.LInfoRowKind != LInfo.LInfoKindSeparator)
            .Select(row => row.LInfoRowKind)
            .ToArray();

    [Fact]
    public void NoMedia_ShowsOneMutedNotice_WithoutSeparator()
    {
        (_, LInfo info) = TInfoBuild();

        IReadOnlyList<LInfoRow> rows = TInterface.TInfoRowsRead(info);

        LInfoRow row = Assert.Single(rows);
        Assert.Equal(LInfo.LInfoKindMuted, row.LInfoRowKind);
    }

    [Fact]
    public void Video_ListsStatusDurationSizeRateCodecs_SeparatedBetweenRows()
    {
        (LViewer viewer, LInfo info) = TInfoBuild();
        int changes = 0;
        TInterface.TInfoAttach(info, () => changes++);

        TInterface.TViewerMediaRaise(
            viewer,
            TInterface.TCargoCreate(
                @"C:\clip.mp4", TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(75), 640, 360), true));

        Assert.Equal(1, changes);
        IReadOnlyList<LInfoRow> rows = TInterface.TInfoRowsRead(info);
        string[] texts = rows
            .Where(row => row.LInfoRowKind == LInfo.LInfoKindText)
            .Select(row => row.LInfoRowText)
            .ToArray();
        Assert.Equal(
            [LInfo.LInfoKindGood, LInfo.LInfoKindGood, LInfo.LInfoKindText, LInfo.LInfoKindText,
                LInfo.LInfoKindText, LInfo.LInfoKindText, LInfo.LInfoKindText],
            TInfoKindsRead(info));
        Assert.Equal(LInfo.LInfoKindSeparator, rows[1].LInfoRowKind);
        Assert.NotEqual(LInfo.LInfoKindSeparator, rows[0].LInfoRowKind);
        Assert.Equal("01:15", texts[0]);
        Assert.Equal("640×360", texts[1]);
        Assert.Equal("25 fps", texts[2]);
        Assert.Equal("H.264", texts[3]);
    }

    [Fact]
    public void AudioOnly_ShowsCodecRateChannels_LongDurationWithHours()
    {
        (LViewer viewer, LInfo info) = TInfoBuild();

        TInterface.TViewerMediaRaise(
            viewer,
            TInterface.TCargoCreate(@"C:\song.flac", TInterface.TViewerAudioCreate(TimeSpan.FromMinutes(61)), false));

        string[] texts = TInterface.TInfoRowsRead(info)
            .Where(row => row.LInfoRowKind == LInfo.LInfoKindText)
            .Select(row => row.LInfoRowText)
            .ToArray();
        Assert.Equal("01:01:00", texts[0]);
        Assert.Equal("AAC", texts[2]);
        Assert.Equal("48 kHz", texts[3]);
        Assert.Equal([LInfo.LInfoKindGood, LInfo.LInfoKindBad], TInfoKindsRead(info).Take(2));
    }

    [Fact]
    public void Errors_ShowMutedShortenedLines_AfterBadStatus()
    {
        (LViewer viewer, LInfo info) = TInfoBuild();

        TInterface.TViewerMediaRaise(
            viewer, TInterface.TCargoErrorCreate(@"C:\broken.mp4", new string('x', 200), null));

        string[] kinds = TInfoKindsRead(info);
        Assert.Equal([LInfo.LInfoKindBad, LInfo.LInfoKindBad, LInfo.LInfoKindMuted], kinds);
        string error = TInterface.TInfoRowsRead(info).Last().LInfoRowText;
        Assert.True(error.Length <= 81);
    }
}
