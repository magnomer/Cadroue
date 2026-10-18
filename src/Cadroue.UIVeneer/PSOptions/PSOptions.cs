using System.Windows;
using Cadroue.Core;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

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

    private readonly QSOptions lsOptions;
    private readonly QSSpectrum lsSpectrum;
    private readonly Action? psOptionsCallback;

    private LPreferenceState lsOptionsDraft => lsOptions.QSOptionsDraft;
    private readonly PSGrabber psOptionsGrabber;

    internal static void PSOptionsShow(Window pOwner, QSOptions lsOptionsOwner, Action? pApplyCallback)
    {
        var psOptions = new PSOptions(pOwner, lsOptionsOwner, pApplyCallback);
        psOptions.ShowDialog();
    }

    private PSOptions(Window pOwner, QSOptions lsOptionsOwner, Action? pApplyCallback)
    {
        lsOptions = lsOptionsOwner;
        lsSpectrum = lsOptions.QSOptionsSpectrumCreate();
        PSOptionsSpectrumAttach();
        psOptionsCallback = pApplyCallback;

        psOptionsStartupMode = PSField.PSModeBuild(
            lsOptionsDraft.LPreferenceStartupMode,
            () => psOptionsStartupPicker?.Invoke(),
            PSOptionsStartupItems);
        psOptionsRecordMode = PSField.PSModeBuild(
            PSOptionsRecordResolve(),
            () => psOptionsRecordNotice?.Invoke(),
            PSOptionsRecordItems);
        psOptionsTabPicker = PSOptionsPickerBuild();
        psMediaBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Startup.OpenLastMedia"),
            lsOptionsDraft.LPreferenceMediaAutomatic);
        psOptionsConfirmBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Confirm.Ask"),
            lsOptionsDraft.LPreferenceConfirmDestructive);
        psRelayClearBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Relay.ClearCheck"),
            lsOptionsDraft.LPreferenceRelayEmpty);
        psOptionsTabsMode = PSField.PSModeBuild(
            PSOptionsTabsResolve(),
            () => { },
            PSOptionsTabsItems);
        psOptionsLanguageCombo = PSCombo.PSComboBuild(lsOptionsDraft.LPreferenceLanguage, PSOptionsLanguagesRead());

        psOptionsEngineMode = PSField.PSModeBuild(
            lsOptions.QSOptionsEngineRead(),
            () => { },
            out psOptionsEngineEnable,
            PSOptionsEngineItems);
        psOptionsEngineEnable("Mpv", lsOptions.QSOptionsMpv.QSOptionsMpvEnabled);
        PSOptionsMpvAttach();
        _ = lsOptions.QSOptionsMpv.QSOptionsMpvUpdate();

        psOptionsAutoplayBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.AutoplayCheck"),
            lsOptionsDraft.LPreferenceAutoplay);
        psOptionsVolumeMode = PSField.PSModeBuild(
            lsOptionsDraft.LPreferenceVolumeMode, () => { }, PSOptionsVolumeItems);
        psOptionsVolumeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceVolume, 0, 100);
        psOptionsWheelMode = PSField.PSModeBuild(
            lsOptionsDraft.LPreferenceWheelAction, () => { }, PSOptionsWheelItems);
        psOptionsDragBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Playback.DragPause"),
            lsOptionsDraft.LPreferenceDragPaused);

        psOptionsOrderMode = PSField.PSModeBuild(
            lsOptionsDraft.LPreferenceTimelineOrder, () => { }, PSOptionsOrderItems);
        psKeyframeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframePixels, 1, 50);
        psKeyframeDelaySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframeDelay, 0, 5000);
        psOptionsOverlapBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.OverlapCheck"),
            lsOptionsDraft.LPreferenceOverlapAllowed);
        psWaveformBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.WaveformCheck"),
            lsOptionsDraft.LPreferenceWaveform);

        psOptionsFailureBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.FailurePause"),
            lsOptionsDraft.LPreferenceFailurePaused);
        psOptionsRetryBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.RetryCheck"),
            lsOptionsDraft.LPreferenceRetryAllowed);
        psOptionsRetrySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceRetryMaximum, 0, 10);

        psOptionsCleanupBox = PSOptionsCheckBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.System.CleanupCheck"),
            lsOptionsDraft.LPreferenceCleanupActive);
        psOptionsCleanupSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceCleanupDays, 1, 365);

        psWorkspaceBox = PSEntry.PSEntryBuild(lsOptionsDraft.LPreferenceWorkspaceFolder, 320);
        psSystemFfmpegBox = PSEntry.PSEntryBuild(lsOptionsDraft.LPreferenceFfmpegFolder, 320);

        PSOptionsWindowApply(pOwner);
        Content = PSOptionsBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSOptionsPlacementKey);
        psOptionsGrabber = new PSGrabber(this);
        psOptionsGrabber.PSGrabberAttach();
        PSOptionsWindowAttach();
    }

    private void PSOptionsSpectrumAttach() => lsSpectrum.QSSpectrumChanged += PSSpectrumActiveApply;

    private void PSOptionsMpvAttach() =>
        lsOptions.QSOptionsMpv.QSOptionsMpvChanged += pEnabled => psOptionsEngineEnable("Mpv", pEnabled);

    private string PSOptionsRecordResolve() =>
        lsOptionsDraft.LPreferenceRecordWorkspace ? "Workspace" : "FileLocation";

    private string PSOptionsTabsResolve() =>
        lsOptionsDraft.LPreferenceVerticalTabs ? "Vertical" : "Horizontal";

    private Control PSOptionsPickerBuild()
    {
        var pPicker = new Control
        {
            MinWidth = 260,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        QPickerControl.QPickerControlAttach(
            pPicker,
            QPicker.QPickerItemsCreate(PSOptionsTabItems, lsOptionsDraft.LPreferenceStartupTabs),
            QLocalizationCatalog.QLocalizationTextRead("Options.Startup.NoTab"));
        return pPicker;
    }

    private void PSOptionsWindowApply(Window pOwner)
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Options.Window.Title");
        Owner = pOwner;
        Width = PSOptionsWidthDefault;
        Height = PSOptionsHeightDefault;
        MinWidth = PSOptionsWidthMinimum;
        MinHeight = PSOptionsHeightMinimum;
        PSSubwindow.PSSubwindowApply(this);
    }

    private void PSOptionsWindowAttach() => Closed += PSOptionsCloseHandle;

    private void PSOptionsWindowDetach() => Closed -= PSOptionsCloseHandle;

    private UIElement PSOptionsBuild()
    {
        TabControl psSheets = PSOptionsSheetBuild();
        PSOptionsSheetAttach(psSheets);
        return PSSubwindow.PSSubwindowBuild(this, PSSheetStripWidth, psSheets);
    }

    private void PSOptionsSheetAttach(TabControl psSheets) =>
        psSheets.SelectionChanged += (_, _) => PSOptionsPageSelect(psSheets);

    private void PSOptionsPageSelect(TabControl psSheets)
    {
        if (psSheets.SelectedIndex >= 0)
        {
            lsOptions.QSOptionsPageSelect((QSOptionsPage)psSheets.SelectedIndex);
        }
    }

    private TabControl PSOptionsSheetBuild()
    {
        return PSSheet.PSSheetControlBuild(
            PSSheetTabWidth,
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.General"),
                PSSheetGeneralIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSGeneralBuild()))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.System"),
                PSSheetSystemIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSSystemBuild()))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Playback"),
                PSSheetPlaybackIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSPlaybackBuild()))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Timeline"),
                PSSheetTimelineIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSTimelineBuild()))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Work"),
                PSSheetWorkIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSWorkBuild()))));
    }

    private UIElement PSOptionsRootBuild(UIElement pSheetContent)
    {
        Button pApply = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Button.Apply"));
        Button pOk = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Button.OK"));
        Button pCancel = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Button.Cancel"));
        PSOptionsFooterAttach(pApply, pOk, pCancel);
        return PSOptionsFrameBuild(pSheetContent, pApply, pOk, pCancel);
    }

    private void PSOptionsFooterAttach(Button pApply, Button pOk, Button pCancel)
    {
        pApply.Click += (_, _) => PSOptionsApply();
        pOk.Click += (_, _) => PSOptionsConfirm();
        pCancel.Click += (_, _) => Close();
    }

    private void PSOptionsConfirm()
    {
        PSOptionsApply();
        Close();
    }

    private static DockPanel PSOptionsFrameBuild(UIElement pSheetContent, Button pApply, Button pOk, Button pCancel)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
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
        PSOptionsDraftUpdate();
        bool psOptionsSaved = lsOptions.QSOptionsApply();
        psOptionsCallback?.Invoke();
        PSSystemMaintenanceUpdate();
        psOptionsRecordNotice?.Invoke();
        if (!psOptionsSaved)
        {
            PSWarning.PSWarningShow(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.Save.FailedTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Options.Save.FailedMessage"));
            return;
        }

        if (lsOptions.QSOptionsRestartCheck())
        {
            PSAnnouncement.PSAnnouncementShow(
                this,
                QLocalizationCatalog.QLocalizationTextRead("Options.Language.RestartTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Options.Language.RestartMessage"));
        }
    }

    private void PSOptionsDraftUpdate()
    {
        lsOptionsDraft.LPreferenceStartupMode = PSField.PSModeTextRead(psOptionsStartupMode);
        lsOptionsDraft.LPreferenceRecordWorkspace = PSOptionsModeCheck(psOptionsRecordMode, "Workspace");
        lsOptionsDraft.LPreferenceStartupTabs = QPickerControl.QPickerControlRead(psOptionsTabPicker).ToList();
        lsOptionsDraft.LPreferenceMediaAutomatic = PSOptionsCheckRead(psMediaBox);
        lsOptionsDraft.LPreferenceConfirmDestructive = PSOptionsCheckRead(psOptionsConfirmBox);
        lsOptionsDraft.LPreferenceRelayEmpty = PSOptionsCheckRead(psRelayClearBox);
        lsOptionsDraft.LPreferenceVerticalTabs = PSOptionsModeCheck(psOptionsTabsMode, "Vertical");
        lsOptionsDraft.LPreferenceLanguage = PSCombo.PSComboTextRead(psOptionsLanguageCombo);

        lsOptionsDraft.LPreferenceAutoplay = PSOptionsCheckRead(psOptionsAutoplayBox);
        lsOptionsDraft.LPreferenceVolumeMode = PSField.PSModeTextRead(psOptionsVolumeMode);
        lsOptionsDraft.LPreferenceVolume = psOptionsVolumeSlider.Value;
        lsOptionsDraft.LPreferenceWheelAction = PSField.PSModeTextRead(psOptionsWheelMode);
        lsOptionsDraft.LPreferenceDragPaused = PSOptionsCheckRead(psOptionsDragBox);
        lsOptionsDraft.LPreferencePreviewEngine = PSField.PSModeTextRead(psOptionsEngineMode);

        lsOptionsDraft.LPreferenceTimelineOrder = PSField.PSModeTextRead(psOptionsOrderMode);
        lsOptionsDraft.LPreferenceKeyframePixels = psKeyframeSlider.Value;
        lsOptionsDraft.LPreferenceKeyframeDelay = psKeyframeDelaySlider.Value;
        lsOptionsDraft.LPreferenceSectionPalette = lsSpectrum.QSSpectrumName;
        lsOptionsDraft.LPreferenceOverlapAllowed = PSOptionsCheckRead(psOptionsOverlapBox);
        lsOptionsDraft.LPreferenceWaveform = PSOptionsCheckRead(psWaveformBox);

        lsOptionsDraft.LPreferenceFailurePaused = PSOptionsCheckRead(psOptionsFailureBox);
        lsOptionsDraft.LPreferenceRetryAllowed = PSOptionsCheckRead(psOptionsRetryBox);
        lsOptionsDraft.LPreferenceRetryMaximum = psOptionsRetrySlider.Value;

        lsOptionsDraft.LPreferenceCleanupActive = PSOptionsCheckRead(psOptionsCleanupBox);
        lsOptionsDraft.LPreferenceCleanupDays = PSOptionsNumberNormalize(psOptionsCleanupSlider.Value);

        lsOptionsDraft.LPreferenceWorkspaceFolder = PSOptionsWorkspaceResolve(psWorkspaceBox.Text);
        lsOptionsDraft.LPreferenceFfmpegFolder = psSystemFfmpegBox.Text;
    }

    private string PSOptionsWorkspaceResolve(string psWorkspaceFolder)
    {
        string psWorkspaceKept = QSOptions.QSOptionsWorkspaceResolve(psWorkspaceFolder, out bool psWorkspaceOccupied);
        if (!psWorkspaceOccupied)
        {
            return psWorkspaceKept;
        }

        PSOptionsWorkspaceShow(psWorkspaceKept);
        PSWarning.PSWarningShow(
            this,
            QLocalizationCatalog.QLocalizationTextRead("Options.System.Workspace"),
            QLocalizationCatalog.QLocalizationFormat(
                "Options.System.WorkspaceOccupied",
                Cadroue.Infrastructure.LDepot.LDepotRootResolve(psWorkspaceFolder)));
        return psWorkspaceKept;
    }

    private void PSOptionsWorkspaceShow(string psWorkspaceKept) => psWorkspaceBox.Text = psWorkspaceKept;

    private void PSOptionsCloseHandle(object? sender, EventArgs e)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSOptionsPlacementKey);
        psOptionsGrabber.PSGrabberDetach();
        PSOptionsWindowDetach();
    }
}
