using System.Windows;
using System.Windows.Controls;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSAudioPlateBuild()
    {
        var pPanel = new StackPanel();

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
        return PSPlateBuild(pPanel);
    }

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
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };
}
