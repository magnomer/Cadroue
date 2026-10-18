using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsDraft
{
    private readonly QSOptions lsOptions;
    internal readonly PSOptionsTimeline psOptionsTimeline;
    internal readonly PSOptionsGeneral psOptionsGeneral;
    internal readonly PSOptionsPlayback psOptionsPlayback;
    internal readonly PSOptionsWork psOptionsWork;

    internal PSOptionsDraft(QSOptions lsOptionsOwner, PSOptionsSpectrum psOptionsSpectrum)
    {
        lsOptions = lsOptionsOwner;
        psOptionsTimeline = new PSOptionsTimeline
        {
            psOptionsSpectrum = psOptionsSpectrum,
            psOptionsOrderMode = PSMode.PSModeBuild(
                lsOptions.QSOptionsDraft.LPreferenceTimelineOrder, () => { }, PSOptionsTimeline.PSOptionsOrderItems),
            psKeyframeSlider = PSOptionsField.PSOptionsSliderBuild(
                lsOptions.QSOptionsDraft.LPreferenceKeyframePixels, 1, 50),
            psKeyframeDelaySlider = PSOptionsField.PSOptionsSliderBuild(
                lsOptions.QSOptionsDraft.LPreferenceKeyframeDelay, 0, 5000),
            psOptionsOverlapBox = PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.OverlapCheck"),
                lsOptions.QSOptionsDraft.LPreferenceOverlapAllowed),
            psWaveformBox = PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.WaveformCheck"),
                lsOptions.QSOptionsDraft.LPreferenceWaveform)
        };
        psOptionsGeneral = new PSOptionsGeneral(
            PSMode.PSModeBuild(
                lsOptions.QSOptionsDraft.LPreferenceStartupMode,
                () => psOptionsGeneral?.psOptionsStartupPicker?.Invoke(),
                PSOptionsGeneral.PSOptionsStartupItems),
            PSOptionsGeneral.PSOptionsPickerBuild(QPicker.QPickerItemsCreate(
                PSOptionsGeneral.PSOptionsTabItems, lsOptions.QSOptionsDraft.LPreferenceStartupTabs)),
            PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Startup.OpenLastMedia"),
                lsOptions.QSOptionsDraft.LPreferenceMediaAutomatic),
            PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Confirm.Ask"),
                lsOptions.QSOptionsDraft.LPreferenceConfirmDestructive),
            PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Relay.ClearCheck"),
                lsOptions.QSOptionsDraft.LPreferenceRelayEmpty),
            PSMode.PSModeBuild(
                lsOptions.QSOptionsDraft.LPreferenceVerticalTabs ? "Vertical" : "Horizontal",
                () => { },
                PSOptionsGeneral.PSOptionsTabsItems),
            PSCombo.PSComboBuild(
                lsOptions.QSOptionsDraft.LPreferenceLanguage, PSOptionsGeneral.PSOptionsLanguagesRead()));

        Border psOptionsEngineMode = PSMode.PSModeBuild(
            lsOptions.QSOptionsEngineRead(),
            () => { },
            out Action<string, bool> psOptionsEngineEnable,
            PSOptionsPlayback.PSOptionsEngineItems);
        psOptionsEngineEnable("Mpv", lsOptions.QSOptionsMpv.QSOptionsMpvEnabled);
        lsOptions.QSOptionsMpv.QSOptionsMpvChanged += pEnabled => psOptionsEngineEnable("Mpv", pEnabled);
        _ = lsOptions.QSOptionsMpv.QSOptionsMpvUpdate();
        psOptionsPlayback = new PSOptionsPlayback
        {
            psOptionsEngineMode = psOptionsEngineMode,
            psOptionsAutoplayBox = PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Playback.AutoplayCheck"),
                lsOptions.QSOptionsDraft.LPreferenceAutoplay),
            psOptionsVolumeMode = PSMode.PSModeBuild(
                lsOptions.QSOptionsDraft.LPreferenceVolumeMode, () => { }, PSOptionsPlayback.PSOptionsVolumeItems),
            psOptionsVolumeSlider = PSOptionsField.PSOptionsSliderBuild(
                lsOptions.QSOptionsDraft.LPreferenceVolume, 0, 100),
            psOptionsWheelMode = PSMode.PSModeBuild(
                lsOptions.QSOptionsDraft.LPreferenceWheelAction, () => { }, PSOptionsPlayback.PSOptionsWheelItems),
            psOptionsDragBox = PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Playback.DragPause"),
                lsOptions.QSOptionsDraft.LPreferenceDragPaused)
        };
        psOptionsWork = new PSOptionsWork
        {
            psOptionsFailureBox = PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Work.FailurePause"),
                lsOptions.QSOptionsDraft.LPreferenceFailurePaused),
            psOptionsRetryBox = PSOptionsField.PSOptionsCheckBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Work.RetryCheck"),
                lsOptions.QSOptionsDraft.LPreferenceRetryAllowed),
            psOptionsRetrySlider = PSOptionsField.PSOptionsSliderBuild(
                lsOptions.QSOptionsDraft.LPreferenceRetryMaximum, 0, 10)
        };
    }

    internal void PSOptionsDraftUpdate(PSOptionsSystem psOptionsSystem, Window pOwner)
    {
        lsOptions.QSOptionsDraft.LPreferenceStartupMode = PSMode.PSModeTextRead(psOptionsGeneral.psOptionsStartupMode);
        lsOptions.QSOptionsDraft.LPreferenceStartupTabs =
            QPickerControl.QPickerControlRead(psOptionsGeneral.psOptionsTabPicker).ToList();
        lsOptions.QSOptionsDraft.LPreferenceMediaAutomatic =
            PSOptionsField.PSOptionsCheckRead(psOptionsGeneral.psMediaBox);
        lsOptions.QSOptionsDraft.LPreferenceConfirmDestructive =
            PSOptionsField.PSOptionsCheckRead(psOptionsGeneral.psOptionsConfirmBox);
        lsOptions.QSOptionsDraft.LPreferenceRelayEmpty =
            PSOptionsField.PSOptionsCheckRead(psOptionsGeneral.psRelayClearBox);
        lsOptions.QSOptionsDraft.LPreferenceVerticalTabs =
            PSOptionsField.PSOptionsModeCheck(psOptionsGeneral.psOptionsTabsMode, "Vertical");
        lsOptions.QSOptionsDraft.LPreferenceLanguage = PSCombo.PSComboTextRead(psOptionsGeneral.psOptionsLanguageCombo);

        lsOptions.QSOptionsDraft.LPreferenceAutoplay =
            PSOptionsField.PSOptionsCheckRead(psOptionsPlayback.psOptionsAutoplayBox);
        lsOptions.QSOptionsDraft.LPreferenceVolumeMode = PSMode.PSModeTextRead(psOptionsPlayback.psOptionsVolumeMode);
        lsOptions.QSOptionsDraft.LPreferenceVolume = psOptionsPlayback.psOptionsVolumeSlider.Value;
        lsOptions.QSOptionsDraft.LPreferenceWheelAction = PSMode.PSModeTextRead(psOptionsPlayback.psOptionsWheelMode);
        lsOptions.QSOptionsDraft.LPreferenceDragPaused =
            PSOptionsField.PSOptionsCheckRead(psOptionsPlayback.psOptionsDragBox);
        lsOptions.QSOptionsDraft.LPreferencePreviewEngine =
            PSMode.PSModeTextRead(psOptionsPlayback.psOptionsEngineMode);

        lsOptions.QSOptionsDraft.LPreferenceTimelineOrder = PSMode.PSModeTextRead(psOptionsTimeline.psOptionsOrderMode);
        lsOptions.QSOptionsDraft.LPreferenceKeyframePixels = psOptionsTimeline.psKeyframeSlider.Value;
        lsOptions.QSOptionsDraft.LPreferenceKeyframeDelay = psOptionsTimeline.psKeyframeDelaySlider.Value;
        lsOptions.QSOptionsDraft.LPreferenceSectionPalette = psOptionsTimeline.psOptionsSpectrum.PSSpectrumNameRead();
        lsOptions.QSOptionsDraft.LPreferenceOverlapAllowed =
            PSOptionsField.PSOptionsCheckRead(psOptionsTimeline.psOptionsOverlapBox);
        lsOptions.QSOptionsDraft.LPreferenceWaveform =
            PSOptionsField.PSOptionsCheckRead(psOptionsTimeline.psWaveformBox);

        lsOptions.QSOptionsDraft.LPreferenceFailurePaused =
            PSOptionsField.PSOptionsCheckRead(psOptionsWork.psOptionsFailureBox);
        lsOptions.QSOptionsDraft.LPreferenceRetryAllowed =
            PSOptionsField.PSOptionsCheckRead(psOptionsWork.psOptionsRetryBox);
        lsOptions.QSOptionsDraft.LPreferenceRetryMaximum = psOptionsWork.psOptionsRetrySlider.Value;

        psOptionsSystem.psOptionsSidecar.PSSidecarDraftUpdate();
        lsOptions.QSOptionsDraft.LPreferenceCleanupActive =
            PSOptionsField.PSOptionsCheckRead(psOptionsSystem.psOptionsCleanupBox);
        lsOptions.QSOptionsDraft.LPreferenceCleanupDays =
            PSOptionsField.PSOptionsNumberNormalize(psOptionsSystem.psOptionsCleanupSlider.Value);
        lsOptions.QSOptionsDraft.LPreferenceWorkspaceFolder = PSOptionsWorkspaceResolve(
            psOptionsSystem.psOptionsWorkspace, psOptionsSystem.psOptionsWorkspace.psWorkspaceBox.Text, pOwner);
        lsOptions.QSOptionsDraft.LPreferenceFfmpegFolder = psOptionsSystem.psOptionsFfmpeg.PSSystemFfmpegRead();
    }

    private static string PSOptionsWorkspaceResolve(
        PSOptionsSystemWorkspace psOptionsWorkspace, string psWorkspaceFolder, Window pOwner)
    {
        string psWorkspaceKept = QSOptions.QSOptionsWorkspaceResolve(psWorkspaceFolder, out bool psWorkspaceOccupied);
        if (!psWorkspaceOccupied)
        {
            return psWorkspaceKept;
        }

        PSOptionsWorkspaceShow(psOptionsWorkspace, psWorkspaceKept);
        PSWarning.PSWarningShow(
            pOwner,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Workspace"),
            QLocalizationCatalog.QLocalizationFormat(
                "Options.System.WorkspaceOccupied",
                Cadroue.Infrastructure.LDepot.LDepotRootResolve(psWorkspaceFolder)));
        return psWorkspaceKept;
    }

    private static void PSOptionsWorkspaceShow(PSOptionsSystemWorkspace psOptionsWorkspace, string psWorkspaceKept) =>
        psOptionsWorkspace.psWorkspaceBox.Text = psWorkspaceKept;
}
