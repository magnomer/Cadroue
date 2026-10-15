using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private void PSAudioEncoderUpdate()
    {
        bool pAvailable = QSEncoder.QSEncoderAudioCheck(PSCombo.PSComboTextRead(psAudioEncoderCombo));
        psAudioEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;
        if (!pAvailable)
        {
            psAudioEncoderNotice.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Unavailable");
        }
    }

    private void PSAudioContainerHandle()
    {
        string pContainer = PSCombo.PSComboTextRead(psOutputContainerCombo);
        string pCurrent = psAudioEncoderCombo.SelectedItem as string ?? string.Empty;
        string[] pItems = QSEncoder.QSEncoderAudioRead(pContainer, pCurrent);
        psAudioEncoderCombo.ItemsSource = pItems;
        psAudioEncoderCombo.SelectedItem = pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();
        PSAudioEncoderUpdate();
    }

    private UIElement PSAudioPlateBuild()
    {
        var pPanel = new StackPanel();

        var pVerify = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify"),
            84,
            new Thickness(8, 0, 0, 0));
        var pLog = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Result"),
            64,
            new Thickness(6, 0, 0, 0));
        pLog.IsEnabled = lsEncoder.QSEncoderAudioResults.Count > 0;
        ProgressBar pProgress = PSField.PSFieldProgressBuild();
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

            pLog.IsEnabled = lsEncoder.QSEncoderAudioResults.Count > 0;
        };
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, lsEncoder.QSEncoderVerdictCreate(true));
        psAudioEncoderCombo.SelectionChanged += (_, _) => PSAudioSet();
        psAudioRateCombo.SelectionChanged += (_, _) => PSAudioSet();

        psAudioEncodePanel.Children.Add(
            PSField.PSFieldButtonBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Encoder"),
                psAudioEncoderCombo,
                pVerify,
                pLog,
                pProgress));
        psAudioEncodePanel.Children.Add(psAudioEncoderNotice);
        psAudioEncodePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.RateControl"),
                psAudioRateCombo));
        psAudioEncodePanel.Children.Add(psAudioRowsPanel);
        psAudioEncodePanel.Children.Add(psAudioSamplePanel);
        psAudioEncodePanel.Children.Add(psAudioChannelPanel);
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();

        pPanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Stream"),
                psAudioStreamCombo));
        pPanel.Children.Add(
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Mode"), psAudioMode));
        pPanel.Children.Add(psAudioEncodePanel);
        pPanel.Children.Add(psAudioNotice);

        psAudioStreamCombo.SelectionChanged += (_, _) => PSAudioScopeUpdate();

        PSAudioRowsRebuild();
        PSAudioScopeUpdate();
        PSAudioEncoderUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSAudioSet()
    {
        string pEncoder = PSCombo.PSComboTextRead(psAudioEncoderCombo);
        string pRate = PSCombo.PSComboTextRead(psAudioRateCombo);
        if (pEncoder.Length > 0 && pRate.Length > 0)
        {
            lsEncoder.QSEncoderAudioSet(pEncoder, pRate);
        }
    }

    private void PSAudioRowsApply()
    {
        string[] pModeLabels = lsEncoder.QSEncoderAudioCodec.LCapabilityModeLabels;
        if (psAudioRateCombo.ItemsSource is not string[] pShown || !pShown.SequenceEqual(pModeLabels))
        {
            psAudioRateCombo.ItemsSource = pModeLabels;
        }

        psAudioRateCombo.SelectedItem = lsEncoder.QSEncoderAudioRate;
        PSAudioRowsRebuild();
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();
        PSAudioEncoderUpdate();
    }

    private void PSAudioScopeUpdate()
    {
        string pStream = PSCombo.PSComboTextRead(psAudioStreamCombo);
        string pMode = PSField.PSModeTextRead(psAudioMode);

        bool pExcluded = pStream == "Exclude" || pMode == "Exclude";
        bool pCopied = pMode == "Copy";
        bool pEncoded = !pExcluded && !pCopied;

        psAudioEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psAudioNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psAudioNotice.Text = pExcluded
            ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Excluded")
            : QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Copied");
    }

    private static TextBlock PSAudioNoticeBuild() => new()
    {
        Foreground = PSEncoderMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };
}
