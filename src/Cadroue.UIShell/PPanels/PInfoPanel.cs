using Cadroue.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PPanels;

public sealed class PInfoPanel : UserControl
{
    private static readonly SolidColorBrush PInfoPanelTextBrush = new(Color.FromRgb(0x4B, 0x55, 0x63));
    private static readonly SolidColorBrush PInfoPanelMutedBrush = new(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly SolidColorBrush PInfoPanelBorderBrush = new(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly SolidColorBrush PInfoPanelSeparatorBrush = new(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly SolidColorBrush PInfoPanelFfmpegGoodBrush = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoPanelFfmpegBadBrush = new(Color.FromRgb(0xE0, 0x53, 0x53));
    private static readonly SolidColorBrush PInfoPanelPreviewGoodBrush = new(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly SolidColorBrush PInfoPanelPreviewBadBrush = new(Color.FromRgb(0xE0, 0x53, 0x53));
    private PViewerPanel? pInfoPanelViewer;
    private readonly StackPanel pInfoItemPanel;

    public PInfoPanel()
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
        pContentRow.Children.Add(PInfoPanelFilmIconCreate());
        pContentRow.Children.Add(pInfoItemPanel);

        var pRoot = new Border
        {
            Margin = new Thickness(16, 0, 16, 8),
            Padding = new Thickness(14, 8, 14, 8),
            BorderBrush = PInfoPanelBorderBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Child = pContentRow
        };
        Content = pRoot;

        PInfoPanelClear();
    }

    public void PInfoPanelAttach(PViewerPanel? pViewer)
    {
        if (pInfoPanelViewer is not null)
            pInfoPanelViewer.PViewerPanelMediaStatusChange -= PInfoPanelMediaStatusChangeHandle;
        pInfoPanelViewer = pViewer;
        if (pInfoPanelViewer is not null)
            pInfoPanelViewer.PViewerPanelMediaStatusChange += PInfoPanelMediaStatusChangeHandle;
    }

    private void PInfoPanelMediaStatusChangeHandle(LMediaOpenStatus pMediaStatus)
    {
        pInfoItemPanel.Children.Clear();
        PInfoStatusAdd(pMediaStatus.LMediaOpenFfmpegProcessable ? "FFmpeg processable" : "FFmpeg unprocessable",
            pMediaStatus.LMediaOpenFfmpegProcessable ? PInfoPanelFfmpegGoodBrush : PInfoPanelFfmpegBadBrush);
        PInfoStatusAdd(pMediaStatus.LMediaOpenPreviewAvailable ? "Preview available" : "Preview unavailable",
            pMediaStatus.LMediaOpenPreviewAvailable ? PInfoPanelPreviewGoodBrush : PInfoPanelPreviewBadBrush);

        if (pMediaStatus.LMediaOpenMediaInfo is not LMediaInfo pMediaInfo)
        {
            PInfoPanelErrorAdd(pMediaStatus);
            return;
        }

        PInfoTextAdd(PInfoPanelDurationFormat(pMediaInfo.LMediaInfoDuration));
        if (pMediaInfo.LMediaInfoVideoPresent)
        {
            PInfoTextAdd($"{pMediaInfo.LMediaInfoVideoWidth}×{pMediaInfo.LMediaInfoVideoHeight}");
            if (pMediaInfo.LMediaInfoVideoFrameRate > 0)
                PInfoTextAdd($"{pMediaInfo.LMediaInfoVideoFrameRate:0.##} fps");
            PInfoTextAdd(PInfoPanelCodecTextFormat(pMediaInfo.LMediaInfoVideoCodecName));
        }
        else
        {
            PInfoTextAdd("Audio only");
        }

        if (pMediaInfo.LMediaInfoAudioPresent)
        {
            string pKHz = (pMediaInfo.LMediaInfoAudioSampleRate / 1000.0).ToString("0.#");
            PInfoTextAdd(PInfoPanelCodecTextFormat(pMediaInfo.LMediaInfoAudioCodecName));
            PInfoTextAdd($"{pKHz} kHz");
            PInfoTextAdd(PInfoPanelChannelTextFormat(pMediaInfo.LMediaInfoAudioChannels));
        }
        else
        {
            PInfoTextAdd("No audio");
        }
    }

    private void PInfoPanelErrorAdd(LMediaOpenStatus pMediaStatus)
    {
        if (!string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenFfmpegError))
            PInfoTextAdd(PInfoPanelTextShorten($"FFmpeg: {pMediaStatus.LMediaOpenFfmpegError}"), true);
        if (!string.IsNullOrWhiteSpace(pMediaStatus.LMediaOpenPreviewError))
            PInfoTextAdd(PInfoPanelTextShorten($"Preview: {pMediaStatus.LMediaOpenPreviewError}"), true);
    }

    private void PInfoPanelClear()
    {
        pInfoItemPanel.Children.Clear();
        pInfoItemPanel.Children.Add(new TextBlock
        {
            Text = "No media loaded",
            FontSize = 11,
            Foreground = PInfoPanelMutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private void PInfoTextAdd(string pText, bool pMuted = false)
    {
        PInfoSeparatorAddIfNeeded();
        pInfoItemPanel.Children.Add(new TextBlock
        {
            Text = pText,
            FontSize = 11,
            Foreground = pMuted ? PInfoPanelMutedBrush : PInfoPanelTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private void PInfoStatusAdd(string pText, Brush pDotBrush)
    {
        PInfoSeparatorAddIfNeeded();
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
            Foreground = PInfoPanelTextBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        pInfoItemPanel.Children.Add(pRow);
    }

    private void PInfoSeparatorAddIfNeeded()
    {
        if (pInfoItemPanel.Children.Count <= 0) return;
        pInfoItemPanel.Children.Add(new Border
        {
            Width = 1,
            Height = 12,
            Background = PInfoPanelSeparatorBrush,
            Margin = new Thickness(14, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    private static Viewbox PInfoPanelFilmIconCreate()
    {
        var pCanvas = new Canvas { Width = 28, Height = 28 };

        var pCard = new Rectangle
        {
            Width = 21,
            Height = 15.5,
            RadiusX = 2.3,
            RadiusY = 2.3,
            Stroke = PInfoPanelTextBrush,
            StrokeThickness = 2.0,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(pCard, 3.5);
        Canvas.SetTop(pCard, 6.0);
        pCanvas.Children.Add(pCard);

        var pDot = new Ellipse
        {
            Width = 3.4,
            Height = 3.4,
            Fill = PInfoPanelTextBrush
        };
        Canvas.SetLeft(pDot, 8.0);
        Canvas.SetTop(pDot, 10.2);
        pCanvas.Children.Add(pDot);

        var pTopLine = new Line
        {
            X1 = 13.8,
            Y1 = 11.9,
            X2 = 20.2,
            Y2 = 11.9,
            Stroke = PInfoPanelTextBrush,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        pCanvas.Children.Add(pTopLine);

        var pBottomLine = new Line
        {
            X1 = 8.0,
            Y1 = 16.1,
            X2 = 20.2,
            Y2 = 16.1,
            Stroke = PInfoPanelTextBrush,
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        pCanvas.Children.Add(pBottomLine);

        return new Viewbox
        {
            Width = 28,
            Height = 28,
            Margin = new Thickness(0, 0, 12, 0),
            Stretch = Stretch.Uniform,
            Child = pCanvas
        };
    }

    private static string PInfoPanelTextShorten(string pText) =>
        pText.Length <= 80 ? pText : pText[..80] + "…";

    private static string PInfoPanelDurationFormat(TimeSpan pDuration) =>
        pDuration.TotalHours >= 1
            ? $"{(int)pDuration.TotalHours:D2}:{pDuration.Minutes:D2}:{pDuration.Seconds:D2}"
            : $"{pDuration.Minutes:D2}:{pDuration.Seconds:D2}";

    private static string PInfoPanelCodecTextFormat(string pCodecName)
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

    private static string PInfoPanelChannelTextFormat(int pChannelCount) => pChannelCount switch
    {
        1 => "Mono",
        2 => "Stereo",
        _ => $"{pChannelCount}ch"
    };
}
