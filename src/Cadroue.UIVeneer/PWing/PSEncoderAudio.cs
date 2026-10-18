using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoderAudio
{
    private readonly QSEncoderAudio lsEncoderAudio;
    private readonly LPreset lsEncoderDraft;
    private readonly Window psEncoderWindow;
    private readonly PSEncoderAudioRate psEncoderAudioRate;
    private readonly PSEncoderAudioSample psEncoderAudioSample;
    private readonly PSEncoderAudioChannel psEncoderAudioChannel;
    private readonly ComboBox psAudioStreamCombo;
    private readonly Border psAudioMode;
    private readonly ComboBox psAudioEncoderCombo;
    private readonly ComboBox psAudioRateCombo;
    private readonly StackPanel psAudioEncodePanel = new();
    private readonly TextBlock psAudioNotice;
    private readonly TextBlock psAudioEncoderNotice;

    internal PSEncoderAudio(QSEncoderAudio lsAudio, LPreset lsDraft, Window pWindow)
    {
        lsEncoderAudio = lsAudio;
        lsEncoderDraft = lsDraft;
        psEncoderWindow = pWindow;
        psAudioStreamCombo = PSCombo.PSComboBuild(lsDraft.LPresetAudio.LPresetStream,
            new LLocalizationChoice("Include first audio track", "Encoder.Stream.FirstAudio"),
            new LLocalizationChoice("Include all audio tracks", "Encoder.Stream.AllAudio"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psAudioMode = PSField.PSModeBuild(lsDraft.LPresetAudio.LPresetMode, PSAudioScopeUpdate,
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psAudioNotice = PSEncoderNotice.PSEncoderNoticeBuild();
        psAudioEncoderNotice = PSEncoderNotice.PSEncoderErrorBuild();
        psAudioEncoderCombo = PSCombo.PSComboBuild(
            lsAudio.QSEncoderAudioEncoder,
            QSEncoderAudio.QSEncoderAudioRead(lsDraft.LPresetContainer, lsAudio.QSEncoderAudioEncoder));
        PSEncoderChip.PSEncoderChipApply(psAudioEncoderCombo);
        psAudioRateCombo = PSCombo.PSComboBuild(
            lsAudio.QSEncoderAudioRate,
            lsAudio.QSEncoderAudioCodec.LCapabilityModeLabels);
        psEncoderAudioRate = new PSEncoderAudioRate(lsAudio, lsDraft);
        psEncoderAudioSample = new PSEncoderAudioSample(lsAudio, lsDraft);
        psEncoderAudioChannel = new PSEncoderAudioChannel(lsAudio, lsDraft);
    }

    internal void PSAudioDraftSet()
    {
        lsEncoderDraft.LPresetAudio.LPresetStream = PSCombo.PSComboTextRead(psAudioStreamCombo);
        lsEncoderDraft.LPresetAudio.LPresetMode = PSField.PSModeTextRead(psAudioMode);
        lsEncoderDraft.LPresetAudio.LPresetQuality = psEncoderAudioRate.PSAudioQualityRead();
        lsEncoderDraft.LPresetAudio.LPresetSpeed = psEncoderAudioRate.PSAudioSpeedRead();
        lsEncoderDraft.LPresetAudio.LPresetExtras = psEncoderAudioRate.PSAudioExtrasRead();
        lsEncoderDraft.LPresetAudio.LPresetSampleRate = psEncoderAudioSample.PSAudioSampleRead();
        lsEncoderDraft.LPresetAudio.LPresetChannels = psEncoderAudioChannel.PSAudioChannelRead();
    }

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

    internal void PSAudioContainerHandle(string pContainer)
    {
        string pCurrent = PSAudioSelectedRead();
        string[] pItems = QSEncoderAudio.QSEncoderAudioRead(pContainer, pCurrent);
        PSEncoderVerification.PSEncoderComboApply(
            psAudioEncoderCombo, pItems, PSEncoderVerification.PSEncoderChoiceResolve(pItems, pCurrent));
        PSAudioEncoderUpdate();
    }

    private string PSAudioSelectedRead() => psAudioEncoderCombo.SelectedItem as string ?? string.Empty;

    internal UIElement PSAudioPlateBuild()
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
        psEncoderAudioSample.PSAudioSampleRebuild();
        psEncoderAudioChannel.PSAudioChannelRebuild();
        StackPanel pPanel = PSAudioPanelBuild();
        PSAudioStreamAttach();
        psEncoderAudioRate.PSAudioRowsRebuild();
        PSAudioScopeUpdate();
        PSAudioEncoderUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSAudioPlateAttach(Button pVerify, Button pLog, ProgressBar pProgress)
    {
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) => await PSAudioCheckHandle(pVerify, pLog, pProgress, pFeed);
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(psEncoderWindow, lsEncoderAudio.QSEncoderVerdictCreate());
        psAudioEncoderCombo.SelectionChanged += (_, _) => PSAudioSet();
        psAudioRateCombo.SelectionChanged += (_, _) => PSAudioSet();
    }

    private async Task PSAudioCheckHandle(Button pVerify, Button pLog, ProgressBar pProgress, IProgress<double> pFeed)
    {
        PSEncoderVerification.PSEncoderProgressStart(pProgress);
        try
        {
            await PSAudioVerifyHandle(psAudioEncoderCombo, pVerify, pFeed);
        }
        finally
        {
            PSEncoderVerification.PSEncoderProgressStop(pProgress);
        }

        PSAudioResultApply(pLog);
    }

    private async Task PSAudioVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        PSEncoderVerification.PSEncoderBusyApply(pButton);
        IReadOnlyList<string> pAvailable;
        try
        {
            pAvailable = await lsEncoderAudio.QSEncoderAudioScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        string pSelected = lsEncoderAudio.QSEncoderAudioEncoder;
        PSEncoderVerification.PSEncoderComboApply(
            pCombo, pAvailable, PSEncoderVerification.PSEncoderChoiceResolve(pAvailable, pSelected));
        PSAudioEncoderUpdate();
        PSEncoderVerification.PSEncoderIdleApply(pButton);
    }

    private void PSAudioResultApply(Button pLog) => pLog.IsEnabled = PSAudioResultCheck();

    private bool PSAudioResultCheck() => lsEncoderAudio.QSEncoderAudioResults.Count > 0;

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
        psAudioEncodePanel.Children.Add(psEncoderAudioRate.PSAudioRowsPanel);
        psAudioEncodePanel.Children.Add(psEncoderAudioSample.PSAudioSamplePanel);
        psAudioEncodePanel.Children.Add(psEncoderAudioChannel.PSAudioChannelPanel);
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
            lsEncoderAudio.QSEncoderAudioSet(pEncoder, pRate);
        }
    }

    internal void PSAudioRowsApply()
    {
        string[] pModeLabels = lsEncoderAudio.QSEncoderAudioCodec.LCapabilityModeLabels;
        if (!PSEncoderLabelsCheck(psAudioRateCombo, pModeLabels))
        {
            PSAudioLabelsApply(pModeLabels);
        }

        PSAudioRateApply();
        psEncoderAudioRate.PSAudioRowsRebuild();
        psEncoderAudioSample.PSAudioSampleRebuild();
        psEncoderAudioChannel.PSAudioChannelRebuild();
        PSAudioEncoderUpdate();
    }

    private static bool PSEncoderLabelsCheck(ComboBox pCombo, string[] pLabels) =>
        pCombo.ItemsSource is string[] pShown && pShown.SequenceEqual(pLabels);

    private void PSAudioLabelsApply(string[] pModeLabels) => psAudioRateCombo.ItemsSource = pModeLabels;

    private void PSAudioRateApply() => psAudioRateCombo.SelectedItem = lsEncoderAudio.QSEncoderAudioRate;

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
}
