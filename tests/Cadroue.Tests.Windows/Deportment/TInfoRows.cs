using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInfoRows
{
    private static (QViewer, QInfo) TInfoBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        return (viewer, TInterface.TInfoCreate(viewer));
    }

    private static string[] TInfoKindsRead(QInfo info) =>
        TInterface.TInfoRowsRead(info)
            .Where(row => row.LInfoRowKind != LInfoRow.LInfoRowSeparator)
            .Select(row => row.LInfoRowKind)
            .ToArray();

    [Fact]
    public void NoMedia_ShowsOneMutedNotice_WithoutSeparator()
    {
        (_, QInfo info) = TInfoBuild();

        IReadOnlyList<LInfoRow> rows = TInterface.TInfoRowsRead(info);

        LInfoRow row = Assert.Single(rows);
        Assert.Equal(LInfoRow.LInfoRowMuted, row.LInfoRowKind);
    }

    [Fact]
    public void Video_ListsStatusDurationSizeRateCodecs_SeparatedBetweenRows()
    {
        (QViewer viewer, QInfo info) = TInfoBuild();
        int changes = 0;
        TInterface.TInfoAttach(info, () => changes++);

        TInterfaceViewer.TViewerMediaRaise(
            viewer,
            TInterfaceViewer.TCargoCreate(
                @"C:\clip.mp4", TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(75), 640, 360), true));

        Assert.Equal(1, changes);
        IReadOnlyList<LInfoRow> rows = TInterface.TInfoRowsRead(info);
        string[] texts = rows
            .Where(row => row.LInfoRowKind == LInfoRow.LInfoRowText)
            .Select(row => row.LInfoRowCaption)
            .ToArray();
        Assert.Equal(
            [LInfoRow.LInfoRowGood, LInfoRow.LInfoRowGood, LInfoRow.LInfoRowText, LInfoRow.LInfoRowText,
                LInfoRow.LInfoRowText, LInfoRow.LInfoRowText, LInfoRow.LInfoRowText],
            TInfoKindsRead(info));
        Assert.Equal(LInfoRow.LInfoRowSeparator, rows[1].LInfoRowKind);
        Assert.NotEqual(LInfoRow.LInfoRowSeparator, rows[0].LInfoRowKind);
        Assert.Equal("01:15", texts[0]);
        Assert.Equal("640×360", texts[1]);
        Assert.Equal("25 fps", texts[2]);
        Assert.Equal("H.264", texts[3]);
    }

    [Fact]
    public void AudioOnly_ShowsCodecRateChannels_LongDurationWithHours()
    {
        (QViewer viewer, QInfo info) = TInfoBuild();

        TInterfaceViewer.TViewerMediaRaise(
            viewer,
            TInterfaceViewer.TCargoCreate(
                @"C:\song.flac", TInterfaceViewer.TViewerAudioCreate(TimeSpan.FromMinutes(61)), false));

        string[] texts = TInterface.TInfoRowsRead(info)
            .Where(row => row.LInfoRowKind == LInfoRow.LInfoRowText)
            .Select(row => row.LInfoRowCaption)
            .ToArray();
        Assert.Equal("01:01:00", texts[0]);
        Assert.Equal("AAC", texts[2]);
        Assert.Equal("48 kHz", texts[3]);
        Assert.Equal([LInfoRow.LInfoRowGood, LInfoRow.LInfoRowBad], TInfoKindsRead(info).Take(2));
    }

    [Fact]
    public void Errors_ShowMutedShortenedLines_AfterBadStatus()
    {
        (QViewer viewer, QInfo info) = TInfoBuild();

        TInterfaceViewer.TViewerMediaRaise(
            viewer, TInterfaceViewer.TCargoErrorCreate(@"C:\broken.mp4", new string('x', 200), null));

        string[] kinds = TInfoKindsRead(info);
        Assert.Equal([LInfoRow.LInfoRowBad, LInfoRow.LInfoRowBad, LInfoRow.LInfoRowMuted], kinds);
        string error = TInterface.TInfoRowsRead(info).Last().LInfoRowCaption;
        Assert.True(error.Length <= 81);
    }
}
