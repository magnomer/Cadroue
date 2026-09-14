using System.Windows;
using Cadroue.Core;
using Cadroue.Application;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PHouse;
using Cadroue.UIShell.PSCasement;

using static Cadroue.UIShell.PSCasement.PSField;
using static Cadroue.UIShell.PSCasement.PSCombo;
using static Cadroue.UIShell.PSCasement.PSEntry;
using static Cadroue.UIShell.PSCasement.PSFooter;
using static Cadroue.UIShell.PSCasement.PSPlate;
using static Cadroue.UIShell.PSCasement.PSNotice;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions : Window
{
    internal const string PSOptionsPlacementKey = "Options";

    internal const double PSOptionsWidthDefault = 900;
    internal const double PSOptionsHeightDefault = 660;
    internal const double PSOptionsWidthMinimum = 780;
    internal const double PSOptionsHeightMinimum = 520;

    private const double PSSheetTabWidth = 112;
    private const int PSSheetTabCount = 5;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetGeneralIcon = "/PAsset/PTab/PSSheetGeneral.svg";
    private const string PSSheetSystemIcon = "/PAsset/PTab/PSSheetSystem.svg";
    private const string PSSheetPlaybackIcon = "/PAsset/PTab/PSSheetPlayback.svg";
    private const string PSSheetTimelineIcon = "/PAsset/PTab/PSSheetTimeline.svg";
    private const string PSSheetWorkIcon = "/PAsset/PTab/PSSheetWork.svg";

    private readonly LPreferenceState lsOptionsDraft;
    private readonly Action<LPreferenceState>? psOptionsCallback;
    private readonly PSGrabber psOptionsGrabber;

    internal static void PSOptionsShow(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        var psOptions = new PSOptions(pOwner, pApplyCallback);
        psOptions.ShowDialog();
    }

    private PSOptions(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        lsOptionsDraft = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        psOptionsCallback = pApplyCallback;

        psOptionsStartupMode = PSModeBuild(
            lsOptionsDraft.LPreferenceStartupMode,
            () => psOptionsStartupPicker?.Invoke(),
            PSOptionsStartupItems);
        psOptionsRecordMode = PSModeBuild(
            lsOptionsDraft.LPreferenceRecordWorkspace ? "Workspace" : "FileLocation",
            () => psOptionsRecordNotice?.Invoke(),
            PSOptionsRecordItems);
        psOptionsTabPicker = new PPicker(
            PSOptionsTabItems,
            lsOptionsDraft.LPreferenceStartupTabs,
            LLocalization.LLocalizationTextRead("Options.Startup.NoTab"))
        {
            MinWidth = 260,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        psMediaBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Startup.OpenLastMedia"),
            lsOptionsDraft.LPreferenceMediaAutomatic);
        psOptionsConfirmBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Confirm.Ask"),
            lsOptionsDraft.LPreferenceConfirmDestructive);
        psRelayClearBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Relay.ClearCheck"),
            lsOptionsDraft.LPreferenceRelayEmpty);
        psOptionsTabsMode = PSModeBuild(
            lsOptionsDraft.LPreferenceVerticalTabs ? "Vertical" : "Horizontal",
            () => { },
            PSOptionsTabsItems);
        psOptionsLanguageCombo = PSComboBuild(lsOptionsDraft.LPreferenceLanguage, PSOptionsLanguagesRead());

        bool psEngineMpvInstalled = Cadroue.Infrastructure.LMpv.LMpvAvailableCheck();
        bool psEngineMpv = psEngineMpvInstalled
            && string.Equals(lsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal);
        psOptionsEngineMode = PSModeBuild(
            psEngineMpv ? "Mpv" : "Flyleaf",
            () => { },
            out psOptionsEngineEnable,
            PSOptionsEngineItems);
        psOptionsEngineEnable("Mpv", psEngineMpvInstalled);
        _ = PSOptionsEngineUpdate();

        psOptionsAutoplayBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Playback.AutoplayCheck"),
            lsOptionsDraft.LPreferenceAutoplay);
        psOptionsVolumeMode = PSModeBuild(lsOptionsDraft.LPreferenceVolumeMode, () => { }, PSOptionsVolumeItems);
        psOptionsVolumeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceVolume, 0, 100);
        psOptionsWheelMode = PSModeBuild(lsOptionsDraft.LPreferenceWheelAction, () => { }, PSOptionsWheelItems);
        psOptionsDragBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Playback.DragPause"),
            lsOptionsDraft.LPreferenceDragPaused);

        psOptionsOrderMode = PSModeBuild(lsOptionsDraft.LPreferenceTimelineOrder, () => { }, PSOptionsOrderItems);
        psKeyframeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframePixels, 1, 50);
        psKeyframeDelaySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframeDelay, 0, 5000);
        psOptionsOverlapBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Timeline.OverlapCheck"),
            lsOptionsDraft.LPreferenceOverlapAllowed);
        psWaveformBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Timeline.WaveformCheck"),
            lsOptionsDraft.LPreferenceWaveform);

        psOptionsFailureBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Work.FailurePause"),
            lsOptionsDraft.LPreferenceFailurePaused);
        psOptionsRetryBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.Work.RetryCheck"),
            lsOptionsDraft.LPreferenceRetryAllowed);
        psOptionsRetrySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceRetryMaximum, 0, 10);

        psOptionsCleanupBox = PSOptionsCheckBuild(
            LLocalization.LLocalizationTextRead("Options.System.CleanupCheck"),
            lsOptionsDraft.LPreferenceCleanupActive);
        psOptionsCleanupSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceCleanupDays, 1, 365);

        psWorkspaceBox = PSEntryBuild(lsOptionsDraft.LPreferenceWorkspaceFolder, 320);
        psSystemFfmpegBox = PSEntryBuild(lsOptionsDraft.LPreferenceFfmpegFolder, 320);

        Title = LLocalization.LLocalizationTextRead("Options.Window.Title");
        Owner = pOwner;
        Width = PSOptionsWidthDefault;
        Height = PSOptionsHeightDefault;
        MinWidth = PSOptionsWidthMinimum;
        MinHeight = PSOptionsHeightMinimum;
        PSSubwindow.PSSubwindowApply(this);
        Content = PSOptionsBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSOptionsPlacementKey);
        psOptionsGrabber = new PSGrabber(this);
        psOptionsGrabber.PSGrabberAttach();
        Closed += PSOptionsCloseHandle;
    }

    private UIElement PSOptionsBuild() =>
        PSSubwindow.PSSubwindowBuild(this, PSSheetStripWidth, PSSheet.PSSheetControlBuild(
            PSSheetTabWidth,
            PSSheet.PSSheetBuild(
                LLocalization.LLocalizationTextRead("Options.Sheet.General"),
                PSSheetGeneralIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSGeneralBuild()))),
            PSSheet.PSSheetBuild(
                LLocalization.LLocalizationTextRead("Options.Sheet.System"),
                PSSheetSystemIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSSystemBuild()))),
            PSSheet.PSSheetBuild(
                LLocalization.LLocalizationTextRead("Options.Sheet.Playback"),
                PSSheetPlaybackIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSPlaybackBuild()))),
            PSSheet.PSSheetBuild(
                LLocalization.LLocalizationTextRead("Options.Sheet.Timeline"),
                PSSheetTimelineIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSTimelineBuild()))),
            PSSheet.PSSheetBuild(
                LLocalization.LLocalizationTextRead("Options.Sheet.Work"),
                PSSheetWorkIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSWorkBuild())))));

    private UIElement PSOptionsRootBuild(UIElement pSheetContent)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        Button pApply = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Options.Button.Apply"));
        Button pOk = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Options.Button.OK"));
        Button pCancel = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Options.Button.Cancel"));
        pApply.Click += (_, _) => PSOptionsApply();
        pOk.Click += (_, _) => { PSOptionsApply(); Close(); };
        pCancel.Click += (_, _) => Close();
        pFooter.Children.Add(pApply);
        pFooter.Children.Add(pOk);
        pFooter.Children.Add(pCancel);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);
        pRoot.Children.Add(new DockPanel { Margin = new Thickness(18), Children = { pSheetContent } });
        return pRoot;
    }

    private void PSOptionsApply()
    {
        lsOptionsDraft.LPreferenceStartupMode = PSModeTextRead(psOptionsStartupMode);
        lsOptionsDraft.LPreferenceRecordWorkspace =
            string.Equals(PSModeTextRead(psOptionsRecordMode), "Workspace", StringComparison.Ordinal);
        lsOptionsDraft.LPreferenceStartupTabs = psOptionsTabPicker.PPickerSelectionRead().ToList();
        lsOptionsDraft.LPreferenceMediaAutomatic = psMediaBox.IsChecked == true;
        lsOptionsDraft.LPreferenceConfirmDestructive = psOptionsConfirmBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRelayEmpty = psRelayClearBox.IsChecked == true;
        lsOptionsDraft.LPreferenceVerticalTabs =
            string.Equals(PSModeTextRead(psOptionsTabsMode), "Vertical", StringComparison.Ordinal);
        lsOptionsDraft.LPreferenceLanguage = PSComboTextRead(psOptionsLanguageCombo);

        lsOptionsDraft.LPreferenceAutoplay = psOptionsAutoplayBox.IsChecked == true;
        lsOptionsDraft.LPreferenceVolumeMode = PSModeTextRead(psOptionsVolumeMode);
        lsOptionsDraft.LPreferenceVolume = psOptionsVolumeSlider.Value;
        lsOptionsDraft.LPreferenceWheelAction = PSModeTextRead(psOptionsWheelMode);
        lsOptionsDraft.LPreferenceDragPaused = psOptionsDragBox.IsChecked == true;
        lsOptionsDraft.LPreferencePreviewEngine = PSModeTextRead(psOptionsEngineMode);

        lsOptionsDraft.LPreferenceTimelineOrder = PSModeTextRead(psOptionsOrderMode);
        lsOptionsDraft.LPreferenceKeyframePixels = psKeyframeSlider.Value;
        lsOptionsDraft.LPreferenceKeyframeDelay = psKeyframeDelaySlider.Value;
        lsOptionsDraft.LPreferenceSectionPalette = psSpectrumName;
        lsOptionsDraft.LPreferenceOverlapAllowed = psOptionsOverlapBox.IsChecked == true;
        lsOptionsDraft.LPreferenceWaveform = psWaveformBox.IsChecked == true;

        lsOptionsDraft.LPreferenceFailurePaused = psOptionsFailureBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRetryAllowed = psOptionsRetryBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRetryMaximum = psOptionsRetrySlider.Value;

        lsOptionsDraft.LPreferenceCleanupActive = psOptionsCleanupBox.IsChecked == true;
        lsOptionsDraft.LPreferenceCleanupDays = (int)Math.Round(psOptionsCleanupSlider.Value);

        lsOptionsDraft.LPreferenceWorkspaceFolder = PSOptionsWorkspaceResolve(psWorkspaceBox.Text);
        lsOptionsDraft.LPreferenceFfmpegFolder = psSystemFfmpegBox.Text;

        bool psOptionsSaved = LPreference.LPreferenceStateSet(lsOptionsDraft.LPreferenceClone());
        Cadroue.Infrastructure.LRenderer.LRendererEngineSet(
            string.Equals(PSModeTextRead(psOptionsEngineMode), "Mpv", StringComparison.Ordinal)
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf);
        psOptionsCallback?.Invoke(LPreference.LPreferenceStateCurrent);
        PSSystemMaintenanceUpdate();
        psOptionsRecordNotice?.Invoke();
        if (!psOptionsSaved)
        {
            PSWarning.PSWarningShow(
                this,
                LLocalization.LLocalizationTextRead("Options.Save.FailedTitle"),
                LLocalization.LLocalizationTextRead("Options.Save.FailedMessage"));
            return;
        }

        string psLanguageSelected = LLocalization.LLocalizationLanguageNormalize(
            LPreference.LPreferenceStateCurrent.LPreferenceLanguage);
        if (!string.Equals(
                psLanguageSelected,
                LLocalization.LLocalizationLanguageRead(),
                StringComparison.OrdinalIgnoreCase))
        {
            PSAnnouncement.PSAnnouncementShow(
                this,
                LLocalization.LLocalizationTextRead("Options.Language.RestartTitle"),
                LLocalization.LLocalizationTextRead("Options.Language.RestartMessage"));
        }
    }

    private string PSOptionsWorkspaceResolve(string psWorkspaceFolder)
    {
        string psWorkspaceCurrent = LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder;
        string psWorkspaceNext = Cadroue.Infrastructure.LDepot.LDepotRootResolve(psWorkspaceFolder);
        if (string.Equals(
                psWorkspaceNext,
                Cadroue.Infrastructure.LDepot.LDepotRootResolve(psWorkspaceCurrent),
                StringComparison.OrdinalIgnoreCase)
            || !Cadroue.Infrastructure.LDepot.LDepotOccupiedCheck(psWorkspaceNext))
        {
            return psWorkspaceFolder;
        }

        psWorkspaceBox.Text = psWorkspaceCurrent;
        PSWarning.PSWarningShow(
            this,
            LLocalization.LLocalizationTextRead("Options.System.Workspace"),
            LLocalization.LLocalizationFormat("Options.System.WorkspaceOccupied", psWorkspaceNext));
        return psWorkspaceCurrent;
    }

    private void PSOptionsCloseHandle(object? sender, EventArgs e)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSOptionsPlacementKey);
        psOptionsGrabber.PSGrabberDetach();
        Closed -= PSOptionsCloseHandle;
    }
}
