using System.Runtime.InteropServices;
using System.Windows;
using Cadroue.Core;
using Cadroue.Application;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSOptions : Window
{
    private const int PSOptionsDwmPreference = 33;
    private const int PSOptionsDwmRound = 2;
    private const int PSOptionsDwmCaption = 35;
    private const int PSOptionsColor = 0x00F7E8DC;

    internal const string PSOptionsPlacementKey = "Options";

    internal const double PSOptionsWidthDefault = 900;
    internal const double PSOptionsHeightDefault = 660;
    internal const double PSOptionsWidthMinimum = 780;
    internal const double PSOptionsHeightMinimum = 520;

    private const double PSSheetTabWidth = 112;
    private const int PSSheetTabCount = 5;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetGeneralIcon = "/PAssets/PTabs/PSSheetGeneral.svg";
    private const string PSSheetSystemIcon = "/PAssets/PTabs/PSSheetSystem.svg";
    private const string PSSheetPlaybackIcon = "/PAssets/PTabs/PSSheetPlayback.svg";
    private const string PSSheetTimelineIcon = "/PAssets/PTabs/PSSheetTimeline.svg";
    private const string PSSheetWorkIcon = "/PAssets/PTabs/PSSheetWork.svg";

    private static readonly LLocalizationChoice[] PSOptionsTabItems =
    {
        new("Split", "Tab.Split"),
        new("Edit", "Tab.Edit"),
        new("Audio", "Tab.Audio"),
        new("Convert", "Tab.Convert"),
        new("Merge", "Tab.Merge"),
        new("Worklist", "Tab.Worklist")
    };

    private static readonly LLocalizationChoice[] PSOptionsVolumeItems =
    {
        new("Unified", "Options.Playback.VolumeUnified"),
        new("PerTab", "Options.Playback.VolumePerTab")
    };

    private static readonly LLocalizationChoice[] PSOptionsWheelItems =
    {
        new("Seek", "Options.Playback.Seek"),
        new("Zoom", "Options.Playback.Zoom"),
        new("Volume", "Options.Playback.Volume")
    };

    private static readonly LLocalizationChoice[] PSOptionsOrderItems =
    {
        new("MapFirst", "Options.Timeline.MapTop"),
        new("ViewfinderFirst", "Options.Timeline.ViewfinderTop")
    };

    private static readonly LLocalizationChoice[] PSOptionsStartupItems =
    {
        new("LastSession", "Options.Startup.LastSession"),
        new("DefaultTab", "Options.Startup.DefaultTab")
    };

    private static readonly LLocalizationChoice[] PSOptionsRecordItems =
    {
        new("FileLocation", "Options.Record.FileLocation"),
        new("Workspace", "Options.Record.Workspace")
    };

    private static readonly LLocalizationChoice[] PSOptionsTabsItems =
    {
        new("Horizontal", "Options.Layout.TabsHorizontal"),
        new("Vertical", "Options.Layout.TabsVertical")
    };

    private static readonly LLocalizationChoice[] PSOptionsEngineItems =
    {
        new("Flyleaf", "Options.Playback.EngineFlyleaf"),
        new("Mpv", "Options.Playback.EngineMpv")
    };

    private readonly LPreferenceState lsOptionsDraft;
    private readonly Action<LPreferenceState>? psOptionsCallback;
    private readonly PSGrabber psOptionsGrabber;

    private readonly Border psOptionsStartupMode;
    private readonly Border psOptionsRecordMode;
    private Action? psOptionsRecordNotice;
    private Action? psOptionsStartupPicker;
    private readonly PPicker psOptionsTabPicker;
    private readonly CheckBox psMediaBox;
    private readonly CheckBox psOptionsConfirmBox;
    private readonly CheckBox psRelayClearBox;
    private readonly Border psOptionsTabsMode;
    private readonly ComboBox psOptionsLanguageCombo;

    private readonly Border psOptionsEngineMode;
    private readonly Action<string, bool> psOptionsEngineEnable;
    private readonly CheckBox psOptionsAutoplayBox;
    private readonly Border psOptionsVolumeMode;
    private readonly Slider psOptionsVolumeSlider;
    private readonly Border psOptionsWheelMode;
    private readonly CheckBox psOptionsDragBox;

    private readonly Border psOptionsOrderMode;
    private readonly Slider psKeyframeSlider;
    private readonly Slider psKeyframeDelaySlider;
    private readonly CheckBox psOptionsOverlapBox;
    private readonly CheckBox psWaveformBox;

    private readonly CheckBox psOptionsFailureBox;
    private readonly CheckBox psOptionsRetryBox;
    private readonly Slider psOptionsRetrySlider;
    private readonly CheckBox psOptionsCleanupBox;
    private readonly Slider psOptionsCleanupSlider;

    internal static void PSOptionsShow(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        var psOptions = new PSOptions(pOwner, pApplyCallback);
        psOptions.ShowDialog();
    }

    private PSOptions(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        lsOptionsDraft = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        psOptionsCallback = pApplyCallback;

        psOptionsStartupMode = PSModeBuild(lsOptionsDraft.LPreferenceStartupMode, () => psOptionsStartupPicker?.Invoke(), PSOptionsStartupItems);
        psOptionsRecordMode = PSModeBuild(lsOptionsDraft.LPreferenceRecordWorkspace ? "Workspace" : "FileLocation", () => psOptionsRecordNotice?.Invoke(), PSOptionsRecordItems);
        psOptionsTabPicker = new PPicker(PSOptionsTabItems, lsOptionsDraft.LPreferenceStartupTabs, LLocalization.LLocalizationTextRead("Options.Startup.NoTab"))
        {
            MinWidth = 260,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        psMediaBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Startup.OpenLastMedia"), lsOptionsDraft.LPreferenceMediaAutomatic);
        psOptionsConfirmBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Confirm.Ask"), lsOptionsDraft.LPreferenceConfirmDestructive);
        psRelayClearBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Relay.ClearCheck"), lsOptionsDraft.LPreferenceRelayEmpty);
        psOptionsTabsMode = PSModeBuild(lsOptionsDraft.LPreferenceVerticalTabs ? "Vertical" : "Horizontal", () => { }, PSOptionsTabsItems);
        psOptionsLanguageCombo = PSComboBuild(lsOptionsDraft.LPreferenceLanguage, PSOptionsLanguagesRead());

        bool psEngineMpvInstalled = Cadroue.Infrastructure.LMpv.LMpvInstalledCheck();
        bool psEngineMpv = psEngineMpvInstalled && string.Equals(lsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal);
        psOptionsEngineMode = PSModeBuild(psEngineMpv ? "Mpv" : "Flyleaf", () => { }, out psOptionsEngineEnable, PSOptionsEngineItems);
        psOptionsEngineEnable("Mpv", psEngineMpvInstalled);

        psOptionsAutoplayBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Playback.AutoplayCheck"), lsOptionsDraft.LPreferenceAutoplay);
        psOptionsVolumeMode = PSModeBuild(lsOptionsDraft.LPreferenceVolumeMode, () => { }, PSOptionsVolumeItems);
        psOptionsVolumeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceVolume, 0, 100);
        psOptionsWheelMode = PSModeBuild(lsOptionsDraft.LPreferenceWheelAction, () => { }, PSOptionsWheelItems);
        psOptionsDragBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Playback.DragPause"), lsOptionsDraft.LPreferenceDragPaused);

        psOptionsOrderMode = PSModeBuild(lsOptionsDraft.LPreferenceTimelineOrder, () => { }, PSOptionsOrderItems);
        psKeyframeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframePixels, 1, 50);
        psKeyframeDelaySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframeDelay, 0, 5000);
        psOptionsOverlapBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Timeline.OverlapCheck"), lsOptionsDraft.LPreferenceOverlapAllowed);
        psWaveformBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Timeline.WaveformCheck"), lsOptionsDraft.LPreferenceWaveform);

        psOptionsFailureBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Work.FailurePause"), lsOptionsDraft.LPreferenceFailurePaused);
        psOptionsRetryBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Work.RetryCheck"), lsOptionsDraft.LPreferenceRetryAllowed);
        psOptionsRetrySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceRetryMaximum, 0, 10);

        psOptionsCleanupBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.System.CleanupCheck"), lsOptionsDraft.LPreferenceCleanupActive);
        psOptionsCleanupSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceCleanupDays, 1, 365);

        psWorkspaceBox = PSEntryBuild(lsOptionsDraft.LPreferenceWorkspaceFolder, 320);
        psSystemFfmpegBox = PSEntryBuild(lsOptionsDraft.LPreferenceFfmpegFolder, 320);

        Title = LLocalization.LLocalizationTextRead("Options.Window.Title");
        Owner = pOwner;
        Width = PSOptionsWidthDefault;
        Height = PSOptionsHeightDefault;
        MinWidth = PSOptionsWidthMinimum;
        MinHeight = PSOptionsHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);
        Content = PSOptionsBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSOptionsPlacementKey);
        psOptionsGrabber = new PSGrabber(this);
        psOptionsGrabber.PSGrabberAttach();
        Closed += PSOptionsCloseHandle;
    }

    private UIElement PSOptionsBuild()
    {
        var pRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };
        pRoot.Children.Add(PSSheet.PSSheetControlBuild(
            PSSheetTabWidth,
            PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Options.Sheet.General"), PSSheetGeneralIcon, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSGeneralBuild()))),
            PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Options.Sheet.System"), PSSheetSystemIcon, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSSystemBuild()))),
            PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Options.Sheet.Playback"), PSSheetPlaybackIcon, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSPlaybackBuild()))),
            PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Options.Sheet.Timeline"), PSSheetTimelineIcon, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSTimelineBuild()))),
            PSSheet.PSSheetBuild(LLocalization.LLocalizationTextRead("Options.Sheet.Work"), PSSheetWorkIcon, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSWorkBuild())))));
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(this, PSSheetStripWidth));
        return pRoot;
    }

    private UIElement PSOptionsRootBuild(UIElement pSheetContent)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12) };
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

    private UIElement PSGeneralBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Startup"),
            PSOptionsStartupBuild(),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.LastMedia"), psMediaBox)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Confirm"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.DestructiveActions"), psOptionsConfirmBox),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.General.DestructiveNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Relay"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.RelayClear"), psRelayClearBox),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.General.RelayClearNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Layout"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.Tabs"), psOptionsTabsMode)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Language"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.Language"), psOptionsLanguageCombo)));
        return pPanel;
    }

    private UIElement PSOptionsStartupBuild()
    {
        psOptionsTabPicker.Margin = new Thickness(12, 0, 0, 0);
        psOptionsTabPicker.VerticalAlignment = VerticalAlignment.Center;

        var pRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pRow.Children.Add(psOptionsStartupMode);
        pRow.Children.Add(psOptionsTabPicker);

        void PSOptionsPickerUpdate() =>
            psOptionsTabPicker.Visibility = string.Equals(PSModeTextRead(psOptionsStartupMode), "DefaultTab", StringComparison.Ordinal)
                ? Visibility.Visible
                : Visibility.Collapsed;
        PSOptionsPickerUpdate();
        psOptionsStartupPicker = PSOptionsPickerUpdate;

        return PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.OpenWith"), pRow);
    }

    private UIElement PSPlaybackBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlaybackEngineBuild());
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Autoplay"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.Autoplay"), psOptionsAutoplayBox)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.VolumePlate"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.VolumeMode"), psOptionsVolumeMode),
            PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.DefaultVolume"), psOptionsVolumeSlider, "%")));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Mousewheel"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.OverTimeline"), psOptionsWheelMode)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Dragging"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.WhileDragging"), psOptionsDragBox)));
        return pPanel;
    }

    private UIElement PSPlaybackEngineBuild()
    {
        return PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Preview"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.Engine"), psOptionsEngineMode),
            PSSystemFlyleafBuild(),
            PSSystemMpvBuild(),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.Playback.MpvEditNotice")));
    }

    private UIElement PSWorkBuild()
    {
        UIElement pRetryRow = PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.RetryLimit"), psOptionsRetrySlider, string.Empty);
        pRetryRow.IsEnabled = psOptionsRetryBox.IsChecked == true;
        psOptionsRetryBox.Checked += (_, _) => pRetryRow.IsEnabled = true;
        psOptionsRetryBox.Unchecked += (_, _) => pRetryRow.IsEnabled = false;

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Work.Failure"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.OnFailure"), psOptionsFailureBox),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.Retry"), psOptionsRetryBox),
            pRetryRow));
        return pPanel;
    }

    private void PSOptionsApply()
    {
        lsOptionsDraft.LPreferenceStartupMode = PSModeTextRead(psOptionsStartupMode);
        lsOptionsDraft.LPreferenceRecordWorkspace = string.Equals(PSModeTextRead(psOptionsRecordMode), "Workspace", StringComparison.Ordinal);
        lsOptionsDraft.LPreferenceStartupTabs = psOptionsTabPicker.PPickerSelectionRead().ToList();
        lsOptionsDraft.LPreferenceMediaAutomatic = psMediaBox.IsChecked == true;
        lsOptionsDraft.LPreferenceConfirmDestructive = psOptionsConfirmBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRelayEmpty = psRelayClearBox.IsChecked == true;
        lsOptionsDraft.LPreferenceVerticalTabs = string.Equals(PSModeTextRead(psOptionsTabsMode), "Vertical", StringComparison.Ordinal);
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

        lsOptionsDraft.LPreferenceWorkspaceFolder = psWorkspaceBox.Text;
        lsOptionsDraft.LPreferenceFfmpegFolder = psSystemFfmpegBox.Text;

        bool psOptionsSaved = LPreference.LPreferenceStateSet(lsOptionsDraft.LPreferenceClone());
        Cadroue.Infrastructure.LRenderer.LRendererEngineSet(
            string.Equals(PSModeTextRead(psOptionsEngineMode), "Mpv", StringComparison.Ordinal)
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf);
        psOptionsCallback?.Invoke(LPreference.LPreferenceStateCurrent);
        if (!psOptionsSaved)
        {
            MessageBox.Show(
                this,
                LLocalization.LLocalizationTextRead("Options.Save.FailedMessage"),
                LLocalization.LLocalizationTextRead("Options.Save.FailedTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string psLanguageSelected = LLocalization.LLocalizationLanguageNormalize(
            LPreference.LPreferenceStateCurrent.LPreferenceLanguage);
        if (!string.Equals(
                psLanguageSelected,
                LLocalization.LLocalizationLanguageRead(),
                StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                this,
                LLocalization.LLocalizationTextRead("Options.Language.RestartMessage"),
                LLocalization.LLocalizationTextRead("Options.Language.RestartTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private static LLocalizationChoice[] PSOptionsLanguagesRead() =>
        LLocalization.LLocalizationLanguagesRead()
            .Select(pLanguage => new LLocalizationChoice(
                pLanguage.Key,
                "Localization.Language.Name",
                pLanguage.Value))
            .ToArray();

    private static CheckBox PSOptionsCheckBuild(string pLabel, bool pChecked)
    {
        var pCheck = new CheckBox
        {
            Content = pLabel,
            IsChecked = pChecked,
            VerticalAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pCheck);
        return pCheck;
    }

    private static Slider PSOptionsSliderBuild(double pValue, double pMinimum, double pMaximum)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            Value = Math.Clamp(pValue, pMinimum, pMaximum),
            Width = 260
        };
        PSlider.PSliderApply(pSlider);
        return pSlider;
    }

    private static UIElement PSOptionsFieldBuild(string pLabel, Slider pSlider, string pUnit)
    {
        var pValueText = new TextBlock
        {
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            MinWidth = 48,
            Text = PSOptionsNumberFormat(pSlider.Value) + pUnit
        };
        pSlider.ValueChanged += (_, _) => pValueText.Text = PSOptionsNumberFormat(pSlider.Value) + pUnit;

        var pRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pValueText);
        return PSFieldBuild(pLabel, pRow);
    }

    private static string PSOptionsNumberFormat(double pValue) => $"{Math.Round(pValue):0}";

    private void PSOptionsCloseHandle(object? sender, EventArgs e)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSOptionsPlacementKey);
        psOptionsGrabber.PSGrabberDetach();
        Closed -= PSOptionsCloseHandle;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PSOptionsDwmApply();
    }

    private void PSOptionsDwmApply()
    {
        IntPtr psOptionsHandle = new WindowInteropHelper(this).Handle;
        if (psOptionsHandle == IntPtr.Zero)
        {
            return;
        }

        int psOptionsCornerPreference = PSOptionsDwmRound;
        _ = DwmSetWindowAttribute(psOptionsHandle, PSOptionsDwmPreference, ref psOptionsCornerPreference, Marshal.SizeOf<int>());

        int psOptionsCaptionColor = PSOptionsColor;
        _ = DwmSetWindowAttribute(psOptionsHandle, PSOptionsDwmCaption, ref psOptionsCaptionColor, Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);
}
