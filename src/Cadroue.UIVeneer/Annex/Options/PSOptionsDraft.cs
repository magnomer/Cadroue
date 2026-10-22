using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsDraft
{
    private readonly QSOptions lsOptions;
    private readonly QSSpectrum qsSpectrum;
    private readonly Action<bool> psOptionsDraftMpv;
    internal readonly PSOptionsTimeline psOptionsTimeline;
    internal readonly PSOptionsPlayback psOptionsPlayback;
    internal readonly PSOptionsWork psOptionsWork;

    internal PSOptionsDraft(QSOptions lsOptionsOwner, QSSpectrum spectrum, PSSpectrum surface)
    {
        lsOptions = lsOptionsOwner;
        qsSpectrum = spectrum;
        psOptionsTimeline = new PSOptionsTimeline
        {
            psSpectrum = surface,
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
        Border psOptionsEngineMode = PSMode.PSModeBuild(
            lsOptions.QSOptionsEngineRead(),
            () => { },
            out Action<string, bool> psOptionsEngineEnable,
            PSOptionsPlayback.PSOptionsEngineItems);
        psOptionsEngineEnable("Mpv", lsOptions.QSMpv.QSMpvEnabled);
        psOptionsDraftMpv = pEnabled => psOptionsEngineEnable("Mpv", pEnabled);
        lsOptions.QSMpv.QSMpvChanged += psOptionsDraftMpv;
        _ = lsOptions.QSMpv.QSMpvUpdate();
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

    internal void PSOptionsDraftDetach() => lsOptions.QSMpv.QSMpvChanged -= psOptionsDraftMpv;

    internal void PSOptionsDraftUpdate(PSOptionsSystem psOptionsSystem)
    {
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
        lsOptions.QSOptionsDraft.LPreferenceSectionPalette = qsSpectrum.QSSpectrumName;
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
        lsOptions.QSWorkspace.QSWorkspaceCommit();
        lsOptions.QSOptionsDraft.LPreferenceFfmpegFolder = lsOptions.QSRenderer.QSRendererApply();
    }
}
