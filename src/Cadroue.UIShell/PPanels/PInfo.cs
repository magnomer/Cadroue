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
    private static readonly SolidColorBrush PInfoFfmpegGoodBrush = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoFfmpegBadBrush = new(Color.FromRgb(0xE0, 0x53, 0x53));
    private static readonly SolidColorBrush PInfoPreviewGoodBrush = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoPreviewBadBrush = new(Color.FromRgb(0xE0, 0x53, 0x53));
    private PViewer? pInfoViewer;
    private readonly StackPanel pInfoItemPanel;

    public PInfo()
    {
        MinHeight = 50;

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
            Padding = new Thickness(14, 8, 14, 8),
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
        PInfoStatusAdd(pMediaStatus.LMediaOpenFfmpegProcessable ? "FFmpeg processable" : "FFmpeg unprocessable",
            pMediaStatus.LMediaOpenFfmpegProcessable ? PInfoFfmpegGoodBrush : PInfoFfmpegBadBrush);
        PInfoStatusAdd(pMediaStatus.LMediaOpenPreviewAvailable ? "Preview available" : "Preview unavailable",
            pMediaStatus.LMediaOpenPreviewAvailable ? PInfoPreviewGoodBrush : PInfoPreviewBadBrush);

        if (pMediaStatus.LMediaOpenMediaInfo is not LMediaInfo pMediaInfo)
        {
            PInfoErrorAdd(pMediaStatus);
            return;
        }

        PInfoTextAdd(PInfoDurationFormat(pMediaInfo.LMediaInfoDuration));
        if (pMediaInfo.LMediaInfoVideoPresent)
        {
            PInfoTextAdd($"{pMediaInfo.LMediaInfoVideoWidth}×{pMediaInfo.LMediaInfoVideoHeight}");
            if (pMediaInfo.LMediaInfoVideoFrameRate > 0)
                PInfoTextAdd($"{pMediaInfo.LMediaInfoVideoFrameRate:0.##} fps");
            PInfoTextAdd(PInfoCodecFormat(pMediaInfo.LMediaInfoVideoCodecName));
        }
        else
        {
            PInfoTextAdd("Audio only");
        }

        if (pMediaInfo.LMediaInfoAudioPresent)
        {
            string pKHz = (pMediaInfo.LMediaInfoAudioSampleRate / 1000.0).ToString("0.#");
            PInfoTextAdd(PInfoCodecFormat(pMediaInfo.LMediaInfoAudioCodecName));
            PInfoTextAdd($"{pKHz} kHz");
            PInfoTextAdd(PInfoChannelFormat(pMediaInfo.LMediaInfoAudioChannels));
        }
        else
        {
            PInfoTextAdd("No audio");
        }
    }

    private void PInfoErrorAdd(LMediaOpenStatus pMediaStatus)
    {
        if (!string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenFfmpegError))
            PInfoTextAdd(PInfoTextShorten($"FFmpeg: {pMediaStatus.LMediaOpenFfmpegError}"), true);
        if (!string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenPreviewError))
            PInfoTextAdd(PInfoTextShorten($"Preview: {pMediaStatus.LMediaOpenPreviewError}"), true);
    }

    private void PInfoClear()
    {
        pInfoItemPanel.Children.Clear();
        pInfoItemPanel.Children.Add(new TextBlock
        {
            Text = "No media loaded",
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
            Width = 28,
            Height = 28,
            Margin = new Thickness(0, 0, 12, 0),
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
        1 => "Mono",
        2 => "Stereo",
        _ => $"{pChannelCount}ch"
    };
}
