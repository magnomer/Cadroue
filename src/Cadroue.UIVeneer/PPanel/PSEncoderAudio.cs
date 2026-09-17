using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCasement.PSField;
using static Cadroue.UIVeneer.PSCasement.PSCombo;
using static Cadroue.UIVeneer.PSCasement.PSInline;
using static Cadroue.UIVeneer.PSCasement.PSPlate;
using static Cadroue.UIVeneer.PSCasement.PSEntry;
using static Cadroue.UIVeneer.PSCasement.PSNotice;
using static Cadroue.UIVeneer.PSCasement.PSFader;

namespace Cadroue.UIVeneer.PPanel;

internal sealed partial class PSEncoder
{
    private void PSAudioEncoderUpdate()
    {
        bool pAvailable = LSEncoder.LSEncoderAudioCheck(PSComboTextRead(psAudioEncoderCombo));
        psAudioEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;
        if (!pAvailable)
        {
            psAudioEncoderNotice.Text = LLocalization.LLocalizationTextRead("Encoder.Audio.Notice.Unavailable");
        }
    }

    private void PSAudioContainerHandle()
    {
        string pContainer = PSComboTextRead(psOutputContainerCombo);
        string pCurrent = psAudioEncoderCombo.SelectedItem as string ?? string.Empty;
        string[] pItems = LSEncoder.LSEncoderAudioRead(pContainer, pCurrent);
        psAudioEncoderCombo.ItemsSource = pItems;
        psAudioEncoderCombo.SelectedItem = pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();
        PSAudioEncoderUpdate();
    }

    private UIElement PSAudioPlateBuild()
    {
        var pPanel = new StackPanel();

        var pVerify = PSInlineButtonBuild(
            LLocalization.LLocalizationTextRead("Encoder.Button.Verify"),
            84,
            new Thickness(8, 0, 0, 0));
        var pLog = PSInlineButtonBuild(
            LLocalization.LLocalizationTextRead("Encoder.Button.Result"),
            64,
            new Thickness(6, 0, 0, 0));
        pLog.IsEnabled = lsEncoder.LSEncoderAudioResults.Count > 0;
        ProgressBar pProgress = PSFieldProgressBuild();
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) =>
        {
            pProgress.Value = 0;
            pProgress.Visibility = Visibility.Visible;
            try
            {
                await PSAudioVerifyHandle(psAudioEncoderCombo, pVerify, pFeed);
            }
            finally
            {
                pProgress.Visibility = Visibility.Collapsed;
            }

            pLog.IsEnabled = lsEncoder.LSEncoderAudioResults.Count > 0;
        };
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(
            this,
            new LSVerdict(
                LLocalization.LLocalizationTextRead("Encoder.Verification.AudioTitle"),
                lsEncoder.LSEncoderAudioResults));
        psAudioEncoderCombo.SelectionChanged += (_, _) => PSAudioSet();
        psAudioRateCombo.SelectionChanged += (_, _) => PSAudioSet();

        psAudioEncodePanel.Children.Add(
            PSFieldButtonBuild(
                LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Encoder"),
                psAudioEncoderCombo,
                pVerify,
                pLog,
                pProgress));
        psAudioEncodePanel.Children.Add(psAudioEncoderNotice);
        psAudioEncodePanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Audio.Field.RateControl"),
                psAudioRateCombo));
        psAudioEncodePanel.Children.Add(psAudioRowsPanel);
        psAudioEncodePanel.Children.Add(psAudioSamplePanel);
        psAudioEncodePanel.Children.Add(psAudioChannelPanel);
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();

        pPanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Stream"),
                psAudioStreamCombo));
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Audio.Field.Mode"), psAudioMode));
        pPanel.Children.Add(psAudioEncodePanel);
        pPanel.Children.Add(psAudioNotice);

        psAudioStreamCombo.SelectionChanged += (_, _) => PSAudioScopeUpdate();

        PSAudioRowsRebuild();
        PSAudioScopeUpdate();
        PSAudioEncoderUpdate();
        return PSPlateBuild(pPanel);
    }

    private void PSAudioSet()
    {
        string pEncoder = PSComboTextRead(psAudioEncoderCombo);
        string pRate = PSComboTextRead(psAudioRateCombo);
        if (pEncoder.Length > 0 && pRate.Length > 0)
        {
            lsEncoder.LSEncoderAudioSet(pEncoder, pRate);
        }
    }

    private void PSAudioRowsApply()
    {
        string[] pModeLabels = lsEncoder.LSEncoderAudioCodec.LCapabilityModeLabels;
        if (psAudioRateCombo.ItemsSource is not string[] pShown || !pShown.SequenceEqual(pModeLabels))
        {
            psAudioRateCombo.ItemsSource = pModeLabels;
        }

        psAudioRateCombo.SelectedItem = lsEncoder.LSEncoderAudioRate;
        PSAudioRowsRebuild();
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();
        PSAudioEncoderUpdate();
    }

    private void PSAudioScopeUpdate()
    {
        string pStream = PSComboTextRead(psAudioStreamCombo);
        string pMode = PSModeTextRead(psAudioMode);

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
