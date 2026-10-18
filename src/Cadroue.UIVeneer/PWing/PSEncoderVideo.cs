using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoderVideo
{
    private readonly QSEncoderVideo lsEncoderVideo;
    private readonly LPreset lsEncoderDraft;
    private readonly Window psEncoderWindow;
    private readonly PSEncoderVideoRate psEncoderVideoRate;
    private readonly PSEncoderVideoResolution psEncoderVideoResolution;
    private readonly PSEncoderVideoFps psEncoderVideoFps;
    private readonly Border psVideoMode;
    private readonly ComboBox psVideoEncoderCombo;
    private readonly ComboBox psVideoRateCombo;
    private readonly ComboBox psVideoPixelCombo;
    private readonly StackPanel psVideoEncodePanel = new();
    private readonly TextBlock psVideoNotice;
    private readonly TextBlock psVideoEncoderNotice;

    internal PSEncoderVideo(QSEncoderVideo lsVideo, QSEncoderSize lsSize, LPreset lsDraft, Window pWindow)
    {
        lsEncoderVideo = lsVideo;
        lsEncoderDraft = lsDraft;
        psEncoderWindow = pWindow;
        psVideoMode = PSMode.PSModeBuild(lsDraft.LPresetVideo.LPresetMode, PSVideoScopeUpdate,
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Smart", "Encoder.Codec.Smart"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"));
        psVideoEncoderCombo = PSCombo.PSComboBuild(
            lsVideo.QSEncoderVideoEncoder,
            QSEncoderVideo.QSEncoderVideoRead(lsDraft.LPresetContainer, lsVideo.QSEncoderVideoEncoder));
        PSEncoderChip.PSEncoderChipApply(psVideoEncoderCombo);
        psVideoRateCombo = PSCombo.PSComboBuild(
            lsVideo.QSEncoderVideoRate,
            lsVideo.QSEncoderVideoCodec.LCapabilityModeLabels);
        psVideoNotice = PSEncoderNotice.PSEncoderNoticeBuild();
        psVideoEncoderNotice = PSEncoderNotice.PSEncoderErrorBuild();
        psEncoderVideoResolution = new PSEncoderVideoResolution(lsSize, lsDraft.LPresetVideo.LPresetSizeReactive);
        psEncoderVideoFps = new PSEncoderVideoFps(lsDraft.LPresetVideo.LPresetFps);
        psVideoPixelCombo = PSCombo.PSComboBuild(lsDraft.LPresetVideo.LPresetPixelLayout,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("yuv420p", "Encoder.Pixel.Yuv420"),
            new LLocalizationChoice("yuv422p", "Encoder.Pixel.Yuv422"),
            new LLocalizationChoice("yuv444p", "Encoder.Pixel.Yuv444"),
            new LLocalizationChoice("yuv420p10le", "Encoder.Pixel.Yuv420Ten"),
            new LLocalizationChoice("yuv422p10le", "Encoder.Pixel.Yuv422Ten"),
            new LLocalizationChoice("yuv444p10le", "Encoder.Pixel.Yuv444Ten"));
        psEncoderVideoRate = new PSEncoderVideoRate(lsVideo, lsDraft);
    }

    internal void PSVideoDraftSet()
    {
        lsEncoderDraft.LPresetVideo.LPresetMode = PSMode.PSModeTextRead(psVideoMode);
        lsEncoderDraft.LPresetVideo.LPresetQuality = psEncoderVideoRate.PSVideoQualityRead();
        lsEncoderDraft.LPresetVideo.LPresetSpeedPreset = psEncoderVideoRate.PSVideoSpeedRead();
        lsEncoderDraft.LPresetVideo.LPresetSizeReactive = psEncoderVideoResolution.PSVideoReactiveRead();
        lsEncoderDraft.LPresetVideo.LPresetFps = psEncoderVideoFps.PSVideoFpsRead();
        lsEncoderDraft.LPresetVideo.LPresetPixelLayout = PSCombo.PSComboTextRead(psVideoPixelCombo);
        lsEncoderDraft.LPresetVideo.LPresetExtras = psEncoderVideoRate.PSVideoExtrasRead();
    }

    internal void PSVideoSizeApply() => psEncoderVideoResolution.PSVideoSizeApply();

    internal UIElement PSVideoPlateBuild()
    {
        var pVerify = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify"),
            84,
            new Thickness(8, 0, 0, 0));
        var pLog = PSInline.PSInlineButtonBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Result"),
            64,
            new Thickness(6, 0, 0, 0));
        PSVideoLogApply(pLog);
        ProgressBar pProgress = PSFieldProgress.PSFieldProgressBuild();
        PSVideoPlateAttach(pVerify, pLog, pProgress);
        StackPanel pPanel = PSVideoPanelBuild(pVerify, pLog, pProgress);
        psEncoderVideoRate.PSVideoRowsRebuild();
        PSVideoScopeUpdate();
        PSVideoEncoderUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSVideoLogApply(Button pLog) =>
        pLog.IsEnabled = lsEncoderVideo.QSEncoderVideoResults.Count > 0;

    private void PSVideoPlateAttach(Button pVerify, Button pLog, ProgressBar pProgress)
    {
        var pFeed = new Progress<double>(pValue => pProgress.Value = pValue);
        pVerify.Click += async (_, _) => await PSVideoVerifyHandle(pVerify, pLog, pProgress, pFeed);
        pLog.Click += (_, _) => PSVerdict.PSVerdictShow(psEncoderWindow, lsEncoderVideo.QSEncoderVerdictCreate());
        psVideoEncoderCombo.SelectionChanged += (_, _) => PSVideoSet();
        psVideoRateCombo.SelectionChanged += (_, _) => PSVideoSet();
        psEncoderVideoResolution.PSVideoReactiveAttach();
    }

    private async Task PSVideoVerifyHandle(Button pVerify, Button pLog, ProgressBar pProgress, IProgress<double> pFeed)
    {
        PSEncoderVerification.PSEncoderProgressStart(pProgress);
        try
        {
            await PSCodecVerifyHandle(psVideoEncoderCombo, pVerify, pFeed);
        }
        finally
        {
            PSEncoderVerification.PSEncoderProgressStop(pProgress);
        }

        PSVideoLogApply(pLog);
    }

    private StackPanel PSVideoPanelBuild(Button pVerify, Button pLog, ProgressBar pProgress)
    {
        var pPanel = new StackPanel();
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldButtonBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Encoder"),
                psVideoEncoderCombo,
                pVerify,
                pLog,
                pProgress));
        psVideoEncodePanel.Children.Add(psVideoEncoderNotice);
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.RateControl"),
                psVideoRateCombo));
        psVideoEncodePanel.Children.Add(psEncoderVideoRate.PSVideoRowsPanel);
        psEncoderVideoResolution.PSVideoResolutionBuild(psVideoEncodePanel);
        psEncoderVideoFps.PSVideoFpsBuild(psVideoEncodePanel);
        psVideoEncodePanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.PixelFormat"),
                psVideoPixelCombo));

        pPanel.Children.Add(
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Mode"), psVideoMode));
        pPanel.Children.Add(psVideoEncodePanel);
        pPanel.Children.Add(psVideoNotice);
        return pPanel;
    }

    private void PSVideoScopeUpdate()
    {
        string pMode = PSMode.PSModeTextRead(psVideoMode);
        bool pEncoded = PSVideoEncodeCheck(pMode);
        PSVideoScopeApply(pEncoded, pMode);
    }

    private static bool PSVideoEncodeCheck(string pMode) => pMode == "Encode";

    private void PSVideoScopeApply(bool pEncoded, string pMode)
    {
        psVideoEncodePanel.Visibility = pEncoded ? Visibility.Visible : Visibility.Collapsed;
        psVideoNotice.Visibility = pEncoded ? Visibility.Collapsed : Visibility.Visible;
        psVideoNotice.Text = QLocalizationCatalog.QLocalizationTextRead(PSVideoNoticeRead(pMode));
    }

    private string PSVideoNoticeRead(string pMode) => lsEncoderVideo.QSEncoderNoticeResolve(pMode);

    private void PSVideoSet()
    {
        string pEncoder = PSCombo.PSComboTextRead(psVideoEncoderCombo);
        string pRate = PSCombo.PSComboTextRead(psVideoRateCombo);
        if (PSVideoChoiceCheck(pEncoder, pRate))
        {
            lsEncoderVideo.QSEncoderVideoSet(pEncoder, pRate);
        }
    }

    private static bool PSVideoChoiceCheck(string pEncoder, string pRate) => pEncoder.Length > 0 && pRate.Length > 0;

    internal void PSVideoRowsApply()
    {
        string[] pModeLabels = lsEncoderVideo.QSEncoderVideoCodec.LCapabilityModeLabels;
        if (PSVideoLabelsCheck(pModeLabels))
        {
            PSVideoLabelsSet(pModeLabels);
        }

        PSVideoRateSelect();
        psEncoderVideoRate.PSVideoRowsRebuild();
        PSVideoEncoderUpdate();
    }

    private bool PSVideoLabelsCheck(string[] pModeLabels) =>
        psVideoRateCombo.ItemsSource is not string[] pShown || !pShown.SequenceEqual(pModeLabels);

    private void PSVideoLabelsSet(string[] pModeLabels) => psVideoRateCombo.ItemsSource = pModeLabels;

    private void PSVideoRateSelect() => psVideoRateCombo.SelectedItem = lsEncoderVideo.QSEncoderVideoRate;

    private void PSVideoEncoderUpdate()
    {
        bool pAvailable = QSEncoderVideo.QSEncoderVideoCheck(PSCombo.PSComboTextRead(psVideoEncoderCombo));
        PSVideoNoticeApply(pAvailable);
        if (!pAvailable)
        {
            PSVideoReasonSet();
        }
    }

    private void PSVideoNoticeApply(bool pAvailable) =>
        psVideoEncoderNotice.Visibility = pAvailable ? Visibility.Collapsed : Visibility.Visible;

    private void PSVideoReasonSet() =>
        psVideoEncoderNotice.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.Unavailable");

    internal void PSCodecContainerHandle(string pContainer)
    {
        string pCurrent = PSCodecCurrentRead();
        string[] pItems = QSEncoderVideo.QSEncoderVideoRead(pContainer, pCurrent);
        PSEncoderVerification.PSEncoderComboApply(
            psVideoEncoderCombo, pItems, PSEncoderVerification.PSEncoderChoiceResolve(pItems, pCurrent));
        PSVideoEncoderUpdate();
    }

    private string PSCodecCurrentRead() => psVideoEncoderCombo.SelectedItem as string ?? string.Empty;

    private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)
    {
        PSEncoderVerification.PSEncoderBusyApply(pButton);
        IReadOnlyList<string>? pAvailable = await PSCodecScan(pFeed);
        if (pAvailable is null)
        {
            return;
        }

        string pSelected = lsEncoderVideo.QSEncoderVideoEncoder;
        PSEncoderVerification.PSEncoderComboApply(
            pCombo, pAvailable, PSEncoderVerification.PSEncoderChoiceResolve(pAvailable, pSelected));
        PSVideoEncoderUpdate();
        PSEncoderVerification.PSEncoderIdleApply(pButton);
    }

    private async Task<IReadOnlyList<string>?> PSCodecScan(IProgress<double> pFeed)
    {
        try
        {
            return await lsEncoderVideo.QSEncoderVideoScan(pFeed);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
