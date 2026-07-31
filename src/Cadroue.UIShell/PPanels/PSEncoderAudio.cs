using System.Windows;
using System.Windows.Controls;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder
{
    private UIElement PSAudioPlateBuild()
    {
        var pPanel = new StackPanel();

        psAudioEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Encoder"), psAudioEncoderCombo));
        psAudioEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Bitrate"), psAudioBitrateCombo));
        psAudioEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.SampleRate"), psAudioSampleCombo));
        psAudioEncodePanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Channels"), psAudioChannelCombo));

        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Stream"), psAudioStreamCombo));
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Mode"), psAudioModeCombo));
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
            ? LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.Excluded")
            : LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.Copied");
    }

    private static TextBlock PSAudioNoticeBuild() => new()
    {
        Foreground = PSEncoderMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };
}
