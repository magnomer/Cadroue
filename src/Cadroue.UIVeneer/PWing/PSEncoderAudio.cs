using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private void PSAudioEncoderUpdate()
    {
        bool pAvailable = QSEncoderAudio.QSEncoderAudioCheck(PSCombo.PSComboTextRead(psAudioEncoderCombo));
        PSAudioWarningApply(pAvailable);
        if (!pAvailable)
        {
            PSAudioWarningSet();
        }
    }

    private void PSAudioWarningApply(bool pAvailable) =>
        psAudioEncoderNotice.Visibility = PLook.PLookVisible[!pAvailable];

    private void PSAudioWarningSet() =>
        psAudioEncoderNotice.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Unavailable");

    private void PSAudioContainerHandle()
    {
        string pContainer = PSCombo.PSComboTextRead(psOutputContainerCombo);
        string pCurrent = PSAudioSelectedRead();
        string[] pItems = QSEncoderAudio.QSEncoderAudioRead(pContainer, pCurrent);
        PSEncoderComboApply(psAudioEncoderCombo, pItems, PSEncoderChoiceResolve(pItems, pCurrent));
        PSAudioEncoderUpdate();
    }

    private string PSAudioSelectedRead() => psAudioEncoderCombo.SelectedItem as string ?? string.Empty;

    private static string? PSEncoderChoiceResolve(IReadOnlyList<string> pItems, string pCurrent) =>
        pItems.Contains(pCurrent) ? pCurrent : pItems.FirstOrDefault();

    private static void PSEncoderComboApply(ComboBox pCombo, IReadOnlyList<string> pItems, string? pSelected)
    {
        pCombo.ItemsSource = pItems;
        pCombo.SelectedItem = pSelected;
    }

    private UIElement PSAudioPlateBuild()
    {
        Button pVerify = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify"),
            84,
            new Thickness(8, 0, 0, 0));
        Button pLog = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Result"),
            64,
            new Thickness(6, 0, 0, 0));
        PSAudioResultApply(pLog);
        ProgressBar pProgress = PSField.PSFieldProgressBuild();
        PSAudioPlateAttach(pVerify, pLog, pProgress);
        PSAudioEncodeBuild(pVerify, pLog, pProgress);
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();
        StackPanel pPanel = PSAudioPanelBuild();
        PSAudioStreamAttach();
        PSAudioRowsRebuild();
        PSAudioScopeUpdate();
        PSAudioEncoderUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSAudioPlateAttach(Button pVerify, Button pLog, ProgressBar pProgress)
    {
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) => await PSAudioCheckHandle(pVerify, pLog, pProgress, pFeed);
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(this, lsEncoder.QSEncoderAudio.QSEncoderVerdictCreate());
        psAudioEncoderCombo.SelectionChanged += (_, _) => PSAudioSet();
        psAudioRateCombo.SelectionChanged += (_, _) => PSAudioSet();
    }

    private async Task PSAudioCheckHandle(Button pVerify, Button pLog, ProgressBar pProgress, IProgress<double> pFeed)
    {
        PSAudioProgressStart(pProgress);
        try
        {
            await PSAudioVerifyHandle(psAudioEncoderCombo, pVerify, pFeed);
        }
        finally
        {
            PSAudioProgressStop(pProgress);
        }

        PSAudioResultApply(pLog);
    }

    private static void PSAudioProgressStart(ProgressBar pProgress)
    {
        pProgress.Value = 0;
        pProgress.Visibility = Visibility.Visible;
    }

    private static void PSAudioProgressStop(ProgressBar pProgress) => pProgress.Visibility = Visibility.Collapsed;

    private void PSAudioResultApply(Button pLog) => pLog.IsEnabled = PSAudioResultCheck();

    private bool PSAudioResultCheck() => lsEncoder.QSEncoderAudio.QSEncoderAudioResults.Count > 0;

    private void PSAudioEncodeBuild(Button pVerify, Button pLog, ProgressBar pProgress)
    {
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
    }

    private StackPanel PSAudioPanelBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Stream"),
                psAudioStreamCombo));
        pPanel.Children.Add(
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Field.Mode"), psAudioMode));
        pPanel.Children.Add(psAudioEncodePanel);
        pPanel.Children.Add(psAudioNotice);
        return pPanel;
    }

    private void PSAudioStreamAttach() => psAudioStreamCombo.SelectionChanged += (_, _) => PSAudioScopeUpdate();

    private void PSAudioSet()
    {
        string pEncoder = PSCombo.PSComboTextRead(psAudioEncoderCombo);
        string pRate = PSCombo.PSComboTextRead(psAudioRateCombo);
        if (pEncoder.Length > 0 && pRate.Length > 0)
        {
            lsEncoder.QSEncoderAudio.QSEncoderAudioSet(pEncoder, pRate);
        }
    }

    private void PSAudioRowsApply()
    {
        string[] pModeLabels = lsEncoder.QSEncoderAudio.QSEncoderAudioCodec.LCapabilityModeLabels;
        if (!PSEncoderLabelsCheck(psAudioRateCombo, pModeLabels))
        {
            PSAudioLabelsApply(pModeLabels);
        }

        PSAudioRateApply();
        PSAudioRowsRebuild();
        PSAudioSampleRebuild();
        PSAudioChannelRebuild();
        PSAudioEncoderUpdate();
    }

    private static bool PSEncoderLabelsCheck(ComboBox pCombo, string[] pLabels) =>
        pCombo.ItemsSource is string[] pShown && pShown.SequenceEqual(pLabels);

    private void PSAudioLabelsApply(string[] pModeLabels) => psAudioRateCombo.ItemsSource = pModeLabels;

    private void PSAudioRateApply() => psAudioRateCombo.SelectedItem = lsEncoder.QSEncoderAudio.QSEncoderAudioRate;

    private void PSAudioScopeUpdate()
    {
        string pStream = PSCombo.PSComboTextRead(psAudioStreamCombo);
        string pMode = PSField.PSModeTextRead(psAudioMode);
        bool pExcluded = PSAudioExcludedCheck(pStream, pMode);
        bool pEncoded = PSAudioEncodedCheck(pExcluded, pMode);
        PSAudioScopeApply(pEncoded, PSAudioNoticeFormat(pExcluded));
    }

    private static bool PSAudioExcludedCheck(string pStream, string pMode) =>
        pStream == "Exclude" || pMode == "Exclude";

    private static bool PSAudioEncodedCheck(bool pExcluded, string pMode) => !pExcluded && pMode != "Copy";

    private static string PSAudioNoticeFormat(bool pExcluded) =>
        pExcluded
            ? QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Excluded")
            : QLocalizationCatalog.QLocalizationTextRead("Encoder.Audio.Notice.Copied");

    private void PSAudioScopeApply(bool pEncoded, string pNotice)
    {
        psAudioEncodePanel.Visibility = PLook.PLookVisible[pEncoded];
        psAudioNotice.Visibility = PLook.PLookVisible[!pEncoded];
        psAudioNotice.Text = pNotice;
    }

    private static TextBlock PSAudioNoticeBuild() => new()
    {
        Foreground = PSEncoderMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };
}
