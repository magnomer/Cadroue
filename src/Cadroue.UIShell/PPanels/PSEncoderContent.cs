using Cadroue.UIShell.PMainWindow;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{

    private static readonly (string PSCodecText, string[] PSCodecValues)[] PSCodecCandidates =
    [
        ("H.264, x264 / libx264", ["libx264"]), ("H.264, Media Foundation / h264_mf", ["h264_mf"]),
        ("H.264, OpenH264 / libopenh264", ["libopenh264"]), ("H.264, Intel QSV / h264_qsv", ["h264_qsv"]),
        ("H.264, AMD AMF / h264_amf", ["h264_amf"]), ("H.264, NVIDIA NVENC / h264_nvenc", ["h264_nvenc"]),
        ("H.265, x265 / libx265", ["libx265"]), ("H.265, Intel QSV / hevc_qsv", ["hevc_qsv"]),
        ("H.265, AMD AMF / hevc_amf", ["hevc_amf"]), ("H.265, Media Foundation / hevc_mf", ["hevc_mf"]),
        ("H.265, NVIDIA NVENC / hevc_nvenc", ["hevc_nvenc"]), ("H.266/VVC, vvenc / libvvenc", ["libvvenc"]),
        ("AV1, AOM / libaom-av1", ["libaom-av1"]), ("AV1, SVT-AV1 / libsvtav1", ["libsvtav1"]),
        ("AV1, rav1e / librav1e", ["librav1e"]), ("AV1, Intel QSV / av1_qsv", ["av1_qsv"]),
        ("AV1, AMD AMF / av1_amf", ["av1_amf"]), ("AV1, NVIDIA NVENC / av1_nvenc", ["av1_nvenc"]),
        ("VP8, libvpx / libvpx / libvpx-vp8", ["libvpx", "libvpx-vp8"]), ("VP9, libvpx / libvpx-vp9", ["libvpx-vp9"]),
        ("VP9, Intel QSV / vp9_qsv", ["vp9_qsv"]), ("MPEG-4 Part 2, Xvid / libxvid", ["libxvid"]),
        ("MPEG-4 Part 2, native / mpeg4", ["mpeg4"]), ("Theora, libtheora / libtheora", ["libtheora"]),
        ("ProRes, native / prores", ["prores"]), ("ProRes, Anatoliy / prores_aw", ["prores_aw"]),
        ("ProRes, Kostya / prores_ks", ["prores_ks"]), ("FFV1, native / ffv1", ["ffv1"]),
        ("MJPEG, native / mjpeg", ["mjpeg"]), ("JPEG 2000, native / jpeg2000", ["jpeg2000"]),
        ("JPEG 2000, OpenJPEG / libopenjpeg", ["libopenjpeg"]), ("WebP, libwebp / libwebp", ["libwebp"]),
        ("WebP, animated libwebp / libwebp_anim", ["libwebp_anim"]), ("EVC, XEVE / libxeve", ["libxeve"]),
        ("AVS2, xavs2 / libxavs2", ["libxavs2"]), ("APV, OpenAPV / liboapv", ["liboapv"])
    ];

    private UIElement PSEncoderRootBuild(UIElement pTabContent)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12) };
        var pApply = PSFooterButtonBuild("Apply");
        var pOk = PSFooterButtonBuild("OK");
        var pCancel = PSFooterButtonBuild("Cancel");
        pApply.Click += (_, _) => PSEncoderApply();
        pOk.Click += (_, _) => { PSEncoderApply(); DialogResult = true; };
        pCancel.Click += (_, _) => Close();
        pFooter.Children.Add(pApply);
        pFooter.Children.Add(pOk);
        pFooter.Children.Add(pCancel);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);
        pRoot.Children.Add(PSEncoderContentBuild(pTabContent));
        return pRoot;
    }

    private UIElement PSEncoderContentBuild(UIElement pTabContent)
    {
        var pPanel = new DockPanel { Margin = new Thickness(18) };
        pPanel.Children.Add(pTabContent);
        return pPanel;
    }

    private UIElement PSOutputBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSOutputPlateBuild());
        pPanel.Children.Add(PSModePlateBuild());
        return pPanel;
    }

    private UIElement PSVideoBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSVideoPlateBuild());
        return pPanel;
    }

    private UIElement PSAudioBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSAudioPlateBuild());
        return pPanel;
    }

    private UIElement PSOutputPlateBuild()
    {
        var pPanel = new StackPanel();
        var psLocationStatus = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(psEncoderFolderPath) ? "Same as source" : psEncoderFolderPath,
            Foreground = PMutedBrush,
            FontSize = 12,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        PSNameBoxPrepare();
        pPanel.Children.Add(PSFieldBuild("Name", psNameBox));
        pPanel.Children.Add(PSNameRowBuild());
        pPanel.Children.Add(PSLocationFieldBuild(psLocationStatus));
        pPanel.Children.Add(PSFieldBuild("Container", psContainerCombo));
        return PSPlateBuild("Output", pPanel);
    }

    private void PSNameBoxPrepare()
    {
        psNameBox.MinWidth = 320;
        psNameBox.Height = 40;
        psNameBox.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private UIElement PSNameRowBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(0, 8, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = "Elements", Foreground = PMutedBrush, VerticalAlignment = VerticalAlignment.Center });

        var pPanel = new WrapPanel();
        pPanel.Children.Add(PSNameTokenBuild("Original Name", "{OriginalName}"));
        pPanel.Children.Add(PSNameTokenBuild("Section Number", "{SectionNumber}"));
        pPanel.Children.Add(PSNameTokenBuild("Date", "{Date}"));
        pPanel.Children.Add(PSNameTokenBuild("Time", "{Time}"));
        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    private UIElement PSNameTokenBuild(string pLabel, string pToken)
    {
        var pText = new TextBlock
        {
            Text = pLabel,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PTextBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var pBorder = new Border
        {
            MinHeight = 30,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 6, 6),
            Cursor = Cursors.Hand,
            Child = pText
        };

        Point? pDragStart = null;
        bool pDragStarted = false;
        pBorder.PreviewMouseLeftButtonDown += (_, e) =>
        {
            pDragStart = e.GetPosition(null);
            pDragStarted = false;
            pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFA));
        };
        pBorder.MouseEnter += (_, _) =>
        {
            if (!pDragStarted)
            {
                pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            }
        };
        pBorder.MouseLeave += (_, _) => pBorder.Background = Brushes.White;
        pBorder.MouseLeftButtonUp += (_, _) =>
        {
            pBorder.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            if (pDragStarted)
            {
                pDragStarted = false;
                return;
            }

            PSNameTokenInsert(pToken);
        };
        pBorder.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || pDragStart is null || pDragStarted)
            {
                return;
            }

            Point pCurrent = e.GetPosition(null);
            if (Math.Abs(pCurrent.X - pDragStart.Value.X) < 4 && Math.Abs(pCurrent.Y - pDragStart.Value.Y) < 4)
            {
                return;
            }

            pDragStarted = true;
            var pData = new DataObject();
            pData.SetData(PToken.PTokenDataFormat, pToken);
            pData.SetData(DataFormats.Text, pToken);
            _ = DragDrop.DoDragDrop(pBorder, pData, DragDropEffects.Copy);
            pBorder.Background = Brushes.White;
        };
        return pBorder;
    }

    private void PSNameTokenInsert(string pToken)
    {
        psNameBox.PTokenInsert(pToken);
    }

    private UIElement PSLocationFieldBuild(TextBlock psLocationStatus)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = "Location", Foreground = PMutedBrush, VerticalAlignment = VerticalAlignment.Center });

        var pValueGrid = new Grid();
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pValueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        psLocationCombo.SelectionChanged += (_, _) => PSLocationChangeHandle(psLocationCombo, psLocationStatus);
        Grid.SetColumn(psLocationCombo, 0);
        Grid.SetColumn(psLocationStatus, 1);
        pValueGrid.Children.Add(psLocationCombo);
        pValueGrid.Children.Add(psLocationStatus);

        Grid.SetColumn(pValueGrid, 1);
        pGrid.Children.Add(pValueGrid);
        return pGrid;
    }

    private void PSLocationChangeHandle(ComboBox psLocationCombo, TextBlock psLocationStatus)
    {
        if (psLocationCombo.SelectedItem as string != "Custom folder")
        {
            psEncoderFolderPath = null;
            psLocationStatus.Text = "Same as source";
            return;
        }

        var psFolderDialog = new OpenFolderDialog { Title = "Choose export folder", Multiselect = false };
        if (!string.IsNullOrWhiteSpace(psEncoderFolderPath))
        {
            psFolderDialog.InitialDirectory = psEncoderFolderPath;
        }

        bool? psFolderResult = psFolderDialog.ShowDialog(this);
        if (psFolderResult == true && !string.IsNullOrWhiteSpace(psFolderDialog.FolderName))
        {
            psEncoderFolderPath = psFolderDialog.FolderName;
            psLocationStatus.Text = psEncoderFolderPath;
            return;
        }

        if (string.IsNullOrWhiteSpace(psEncoderFolderPath))
        {
            psLocationCombo.SelectedIndex = 0;
            psLocationStatus.Text = "Same as source";
            return;
        }

        psLocationStatus.Text = psEncoderFolderPath;
    }

    private UIElement PSModePlateBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSFieldBuild("Mode", psModeCombo));
        pPanel.Children.Add(PSNoticeBuild("Audio-only output is created in the Video tab by setting Stream to Exclude."));
        return PSPlateBuild("Export Mode", pPanel);
    }

    private static string[] PSCodecItemsRead() =>
        PSCodecCandidates.Select(pCandidate => pCandidate.PSCodecText).ToArray();

    /// <summary>Map an encoder list entry back to the FFmpeg encoder name it selects.</summary>
    private static string PSCodecValueRead(string pText)
    {
        foreach (var pCandidate in PSCodecCandidates)
        {
            if (string.Equals(pCandidate.PSCodecText, pText, StringComparison.Ordinal))
            {
                return pCandidate.PSCodecValues.FirstOrDefault() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton)
    {
        string pSelected = pCombo.SelectedItem as string ?? string.Empty;
        pButton.IsEnabled = false;
        pButton.Content = "Checking";
        var pAvailable = new List<string>();
        var pLog = new StringBuilder();
        pLog.AppendLine($"Verification: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        pLog.AppendLine("Command pattern: ffmpeg -hide_banner -loglevel error -f lavfi -i testsrc2=size=64x64:rate=1 -frames:v 1 -an -c:v <encoder> -f null -");
        foreach (var pCandidate in PSCodecCandidates)
        {
            bool pCandidateAvailable = false;
            pLog.AppendLine();
            pLog.AppendLine(pCandidate.PSCodecText);
            foreach (string pEncoder in pCandidate.PSCodecValues)
            {
                var pResult = await PSCodecCompatibleRead(pEncoder);
                pCandidateAvailable |= pResult.PSCodecSuccess;
                pLog.AppendLine($"  {pEncoder}: {(pResult.PSCodecSuccess ? "OK" : "FAIL")} - {pResult.PSCodecMessage}");
            }
            if (pCandidateAvailable) pAvailable.Add(pCandidate.PSCodecText);
        }

        pCombo.ItemsSource = pAvailable;
        pCombo.SelectedItem = pAvailable.Contains(pSelected) ? pSelected : pAvailable.FirstOrDefault();
        psCodecLog = pLog.ToString();
        pButton.Content = "Verify";
        pButton.IsEnabled = true;
    }

    private static async Task<(bool PSCodecSuccess, string PSCodecMessage)> PSCodecCompatibleRead(string pEncoder)
    {
        using var pProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel error -f lavfi -i testsrc2=size=64x64:rate=1 -frames:v 1 -an -c:v {pEncoder} -f null -",
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true
            }
        };

        try
        {
            pProcess.Start();
            Task<string> pErrorTask = pProcess.StandardError.ReadToEndAsync();
            Task<string> pOutputTask = pProcess.StandardOutput.ReadToEndAsync();
            Task pExitTask = pProcess.WaitForExitAsync();
            if (await Task.WhenAny(pExitTask, Task.Delay(TimeSpan.FromSeconds(6))) != pExitTask)
            {
                pProcess.Kill(true);
                return (false, "timeout after 6 seconds");
            }

            string pMessage = PSCodecLogCompact(await pErrorTask, await pOutputTask);
            return (pProcess.ExitCode == 0, $"exit {pProcess.ExitCode}{pMessage}");
        }
        catch (Exception pException)
        {
            return (false, pException.Message);
        }
    }

    private static string PSCodecLogCompact(string pError, string pOutput)
    {
        string pMessage = string.IsNullOrWhiteSpace(pError) ? pOutput : pError;
        pMessage = pMessage.Replace("\r", " ").Replace("\n", " ").Trim();
        return string.IsNullOrWhiteSpace(pMessage) ? string.Empty : $": {pMessage[..Math.Min(500, pMessage.Length)]}";
    }

    private UIElement PSAudioPlateBuild()
    {
        var pPanel = new StackPanel();

        // Only meaningful when the audio is actually re-encoded.
        psAudioEncodePanel.Children.Add(PSFieldBuild("Encoder", psAudioEncoderCombo));
        psAudioEncodePanel.Children.Add(PSFieldBuild("Bitrate", psAudioBitrateCombo));
        psAudioEncodePanel.Children.Add(PSFieldBuild("Sample rate", psAudioSampleCombo));
        psAudioEncodePanel.Children.Add(PSFieldBuild("Channels", psAudioChannelCombo));

        pPanel.Children.Add(PSFieldBuild("Stream", psAudioStreamCombo));
        pPanel.Children.Add(PSFieldBuild("Mode", psAudioModeCombo));
        pPanel.Children.Add(psAudioEncodePanel);
        pPanel.Children.Add(psAudioNotice);

        psAudioStreamCombo.SelectionChanged += (_, _) => PSAudioScopeUpdate();
        psAudioModeCombo.SelectionChanged += (_, _) => PSAudioScopeUpdate();

        PSAudioScopeUpdate();
        return PSPlateBuild("Audio", pPanel);
    }

    /// <summary>
    /// Mirror of <c>PSVideoScopeUpdate</c> for audio: an excluded stream becomes -an and
    /// a copied one becomes -c:a copy, so the codec rows apply to neither.
    /// </summary>
    private void PSAudioScopeUpdate()
    {
        string pStream = PSComboTextRead(psAudioStreamCombo);
        string pMode = PSComboTextRead(psAudioModeCombo);

        bool pExcluded = pStream == "Exclude" || pMode == "Exclude";
        bool pCopied = pMode == "Copy";
        bool pEncoded = !pExcluded && !pCopied;

        psAudioEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psAudioNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psAudioNotice.Text = pExcluded
            ? "No audio stream is written, so no audio settings apply."
            : "The audio stream is copied as-is, so codec settings do not apply.";
    }

    private static TextBlock PSScopeNoticeBuild() => new()
    {
        Foreground = PMutedBrush,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(130, 2, 0, 4),
        Visibility = Visibility.Collapsed
    };

    private static Border PSPlateBuild(string pTitle, UIElement pContent)
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(new TextBlock { Text = pTitle, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = PTextBrush, Margin = new Thickness(0, 0, 0, 10) });
        pPanel.Children.Add(pContent);
        return new Border
        {
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            Background = PSoftBrush,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = pPanel
        };
    }

    private static UIElement PSFieldBuild(string pLabel, Control pControl)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pLabel, Foreground = PMutedBrush, VerticalAlignment = VerticalAlignment.Center });
        pControl.MinHeight = 28;
        Grid.SetColumn(pControl, 1);
        pGrid.Children.Add(pControl);
        return pGrid;
    }

    private static UIElement PSFieldButtonBuild(string pLabel, Control pControl, params Button[] pButtons)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(new TextBlock { Text = pLabel, Foreground = PMutedBrush, VerticalAlignment = VerticalAlignment.Center });

        var pPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
        pControl.MinHeight = 28;
        pPanel.Children.Add(pControl);
        foreach (Button pButton in pButtons) pPanel.Children.Add(pButton);

        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }

    private static Button PSInlineButtonBuild(string pText, double pWidth, Thickness pMargin) => new()
    {
        Content = pText,
        Width = pWidth,
        Height = 40,
        Margin = pMargin,
        Style = PButton.PButtonWhiteCreate()
    };

    private static ComboBox PSComboBuild(string pSelected, params string[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectedItem = pItems.Contains(pSelected) ? pSelected : pItems.FirstOrDefault();
        return pCombo;
    }

    private static TextBox PSEntryBuild(string pText, double pWidth)
    {
        var pTextBox = new TextBox
        {
            Text = pText,
            Width = pWidth,
            Height = 40,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PTextbox.PTextboxApply(pTextBox);
        return pTextBox;
    }

    private static Button PSFooterButtonBuild(string pText)
    {
        return new Button
        {
            Content = pText,
            Width = 84,
            Margin = new Thickness(4),
            Style = PButton.PButtonWhiteCreate()
        };
    }

    private static string PSComboTextRead(ComboBox pCombo) => pCombo.SelectedItem as string ?? string.Empty;

    private static UIElement PSNoticeBuild(string pText) => new Border
    {
        BorderBrush = new SolidColorBrush(Color.FromRgb(0xBF, 0xD4, 0xF4)),
        BorderThickness = new Thickness(1),
        Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFF)),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(10),
        Margin = new Thickness(130, 2, 0, 2),
        Child = new TextBlock { Text = pText, Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x55, 0x88)), TextWrapping = TextWrapping.Wrap }
    };
}
