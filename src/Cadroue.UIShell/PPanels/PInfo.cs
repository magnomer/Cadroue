using Cadroue.Media;
using Cadroue.UIShell.PAssets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PPanels;

public sealed class PInfo : UserControl
{
    private static readonly SolidColorBrush PInfoTextBrush = new(Color.FromRgb(0x4B, 0x55, 0x63));
    private static readonly SolidColorBrush PInfoMutedBrush = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush PInfoBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PInfoSeparatorBrush = new(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly SolidColorBrush PInfoFfmpegGood = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoFfmpegBad = new(Color.FromRgb(0xE0, 0x53, 0x53));
    private static readonly SolidColorBrush PInfoPreviewGood = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoPreviewBad = new(Color.FromRgb(0xE0, 0x53, 0x53));
    private PViewer? pInfoViewer;
    private readonly StackPanel pInfoItemPanel;

    public PInfo()
    {
        MinHeight = 38;

        pInfoItemPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pContentRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pContentRow.Children.Add(PInfoIconCreate());
        pContentRow.Children.Add(pInfoItemPanel);

        var pRoot = new Border
        {
            Padding = new Thickness(14, 4, 14, 4),
            BorderBrush = PInfoBorderBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Child = pContentRow
        };
        Content = pRoot;

        PInfoClear();
    }

    public void PInfoAttach(PViewer? pViewer)
    {
        if (pInfoViewer is not null)
            pInfoViewer.PViewerMediaChange -= PInfoMediaHandle;
        pInfoViewer = pViewer;
        if (pInfoViewer is not null)
            pInfoViewer.PViewerMediaChange += PInfoMediaHandle;
    }

    private void PInfoMediaHandle(LMediaOpenStatus pMediaStatus)
    {
        pInfoItemPanel.Children.Clear();

        if (string.IsNullOrEmpty(pMediaStatus.LMediaOpenSourcePath))
        {
            PInfoStatusAdd(LLocalization.LLocalizationTextRead(LMediaInfo.LMediaFfprobeExist() ? "Info.FFmpeg.Ready" : "Info.FFmpeg.Missing"), PInfoMutedBrush);
            return;
        }

        PInfoStatusAdd(LLocalization.LLocalizationTextRead(pMediaStatus.LMediaOpenFfmpegProcessable ? "Info.FFmpeg.Processable" : "Info.FFmpeg.Unprocessable"),
            pMediaStatus.LMediaOpenFfmpegProcessable ? PInfoFfmpegGood : PInfoFfmpegBad);
        PInfoStatusAdd(LLocalization.LLocalizationTextRead(pMediaStatus.LMediaOpenPreviewAvailable ? "Info.Preview.Available" : "Info.Preview.Unavailable"),
            pMediaStatus.LMediaOpenPreviewAvailable ? PInfoPreviewGood : PInfoPreviewBad);

        if (pMediaStatus.LMediaOpenMediaInfo is not LMediaInfo pMediaInfo)
        {
            PInfoErrorAdd(pMediaStatus);
            return;
        }

        PInfoTextAdd(PInfoDurationFormat(pMediaInfo.LMediaInfoDuration));
        if (pMediaInfo.LMediaVideoPresent)
        {
            PInfoTextAdd($"{pMediaInfo.LMediaVideoWidth}×{pMediaInfo.LMediaVideoHeight}");
            if (pMediaInfo.LMediaVideoRate > 0)
                PInfoTextAdd($"{pMediaInfo.LMediaVideoRate:0.##} fps");
            PInfoTextAdd(PInfoCodecFormat(pMediaInfo.LMediaVideoCodec));
        }
        else
        {
            PInfoTextAdd(LLocalization.LLocalizationTextRead("Info.Audio.Only"));
        }

        if (pMediaInfo.LMediaAudioPresent)
        {
            string pKHz = (pMediaInfo.LMediaSampleRate / 1000.0).ToString("0.#");
            PInfoTextAdd(PInfoCodecFormat(pMediaInfo.LMediaAudioCodec));
            PInfoTextAdd($"{pKHz} kHz");
            PInfoTextAdd(PInfoChannelFormat(pMediaInfo.LMediaAudioChannels));
        }
        else
        {
            PInfoTextAdd(LLocalization.LLocalizationTextRead("Info.Audio.None"));
        }
    }

    private void PInfoErrorAdd(LMediaOpenStatus pMediaStatus)
    {
        if (!string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenFfmpegError))
            PInfoTextAdd(PInfoTextShorten(LLocalization.LLocalizationFormat("Info.Error.FFmpeg", pMediaStatus.LMediaOpenFfmpegError)), true);
        if (!string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenPreviewError))
            PInfoTextAdd(PInfoTextShorten(LLocalization.LLocalizationFormat("Info.Error.Preview", pMediaStatus.LMediaOpenPreviewError)), true);
    }

    private void PInfoClear()
    {
        pInfoItemPanel.Children.Clear();
        pInfoItemPanel.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Source.Empty.Notice"),
            FontSize = 11,
            Foreground = PInfoMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private void PInfoTextAdd(string pText, bool pMuted = false)
    {
        PInfoSeparatorAdd();
        pInfoItemPanel.Children.Add(new TextBlock
        {
            Text = pText,
            FontSize = 11,
            Foreground = pMuted ? PInfoMutedBrush : PInfoTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private void PInfoStatusAdd(string pText, Brush pDotBrush)
    {
        PInfoSeparatorAdd();
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = pDotBrush,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        pRow.Children.Add(new TextBlock
        {
            Text = pText,
            FontSize = 11,
            Foreground = PInfoTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        pInfoItemPanel.Children.Add(pRow);
    }

    private void PInfoSeparatorAdd()
    {
        if (pInfoItemPanel.Children.Count <= 0) return;
        pInfoItemPanel.Children.Add(new Border
        {
            Width = 1,
            Height = 12,
            Background = PInfoSeparatorBrush,
            Margin = new Thickness(14, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private static Image PInfoIconCreate()
    {
        return new Image
        {
            Width = 20,
            Height = 20,
            Margin = new Thickness(0, 0, 10, 0),
            Stretch = Stretch.Uniform,
            Source = PIcon.PIconRead("/PAssets/PPanels/PInfo.svg")
        };
    }

    private static string PInfoTextShorten(string pText) =>
        pText.Length <= 80 ? pText : pText[..80] + "…";

    private static string PInfoDurationFormat(TimeSpan pDuration) =>
        pDuration.TotalHours >= 1
            ? $"{(int)pDuration.TotalHours:D2}:{pDuration.Minutes:D2}:{pDuration.Seconds:D2}"
            : $"{pDuration.Minutes:D2}:{pDuration.Seconds:D2}";

    private static string PInfoCodecFormat(string pCodecName)
    {
        if (string.IsNullOrWhiteSpace(pCodecName)) return string.Empty;
        return pCodecName.Trim().ToLowerInvariant() switch
        {
            "h264" or "avc" => "H.264",
            "hevc" or "h265" => "H.265",
            "aac" => "AAC",
            "mp3" => "MP3",
            "flac" => "FLAC",
            "pcm_s16le" => "PCM",
            _ => pCodecName.ToUpperInvariant()
        };
    }

    private static string PInfoChannelFormat(int pChannelCount) => pChannelCount switch
    {
        1 => LLocalization.LLocalizationTextRead("Encoder.Value.Mono"),
        2 => LLocalization.LLocalizationTextRead("Encoder.Value.Stereo"),
        _ => $"{pChannelCount}ch"
    };
}
