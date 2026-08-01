using System.Runtime.InteropServices;
using System.Windows;
using Cadroue.Core;
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
        new("PerTab", "Options.Playback.PerTabVolume"),
        new("Unified", "Options.Playback.UnifiedVolume")
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

    private readonly LPreferenceState lsOptionsDraft;
    private readonly Action<LPreferenceState>? psOptionsCallback;
    private readonly PSGrabber psOptionsGrabber;

    private readonly RadioButton psOptionsStartupSession;
    private readonly RadioButton psOptionsStartupDefault;
    private readonly RadioButton psOptionsRecordBeside;
    private readonly RadioButton psOptionsRecordWorkspace;
    private readonly PPicker psOptionsTabPicker;
    private readonly CheckBox psMediaBox;
    private readonly CheckBox psOptionsConfirmBox;
    private readonly CheckBox psRelayClearBox;
    private readonly ComboBox psOptionsLanguageCombo;

    private readonly CheckBox psOptionsAutoplayBox;
    private readonly ComboBox psOptionsVolumeCombo;
    private readonly Slider psOptionsVolumeSlider;
    private readonly ComboBox psOptionsWheelCombo;
    private readonly CheckBox psOptionsDragBox;

    private readonly ComboBox psOptionsOrderCombo;
    private readonly Slider psKeyframeSlider;
    private readonly CheckBox psOptionsOverlapBox;
    private readonly CheckBox psWaveformBox;

    private readonly Slider psOptionsParallelSlider;
    private readonly CheckBox psOptionsFailureBox;
    private readonly CheckBox psOptionsRetryBox;
    private readonly Slider psOptionsRetrySlider;

    internal static void PSOptionsShow(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        var psOptions = new PSOptions(pOwner, pApplyCallback);
        psOptions.ShowDialog();
    }

    private PSOptions(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        lsOptionsDraft = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        psOptionsCallback = pApplyCallback;

        psOptionsStartupSession = PSOptionsRadioBuild(LLocalization.LLocalizationTextRead("Options.Startup.LastSession"), lsOptionsDraft.LPreferenceStartupMode == "LastSession");
        psOptionsStartupDefault = PSOptionsRadioBuild(LLocalization.LLocalizationTextRead("Options.Startup.DefaultTab"), lsOptionsDraft.LPreferenceStartupMode == "DefaultTab");
        psOptionsRecordBeside = PSOptionsRadioBuild(LLocalization.LLocalizationTextRead("Options.Record.FileLocation"), !lsOptionsDraft.LPreferenceRecordWorkspace, "PSOptionsRecord");
        psOptionsRecordWorkspace = PSOptionsRadioBuild(LLocalization.LLocalizationTextRead("Options.Record.Workspace"), lsOptionsDraft.LPreferenceRecordWorkspace, "PSOptionsRecord");
        psOptionsTabPicker = new PPicker(PSOptionsTabItems, lsOptionsDraft.LPreferenceStartupTabs, LLocalization.LLocalizationTextRead("Options.Startup.NoTab"))
        {
            MinWidth = 260,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        psMediaBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Startup.OpenLastMedia"), lsOptionsDraft.LPreferenceMediaAutomatic);
        psOptionsConfirmBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Confirm.Ask"), lsOptionsDraft.LPreferenceConfirmDestructive);
        psRelayClearBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Relay.ClearCheck"), lsOptionsDraft.LPreferenceRelayEmpty);
        psOptionsLanguageCombo = PSComboBuild(lsOptionsDraft.LPreferenceLanguage, PSOptionsLanguagesRead());

        psOptionsAutoplayBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Playback.AutoplayCheck"), lsOptionsDraft.LPreferenceAutoplay);
        psOptionsVolumeCombo = PSComboBuild(lsOptionsDraft.LPreferenceVolumeMode, PSOptionsVolumeItems);
        psOptionsVolumeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceVolume, 0, 100);
        psOptionsWheelCombo = PSComboBuild(lsOptionsDraft.LPreferenceWheelAction, PSOptionsWheelItems);
        psOptionsDragBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Playback.DragPause"), lsOptionsDraft.LPreferenceDragPaused);

        psOptionsOrderCombo = PSComboBuild(lsOptionsDraft.LPreferenceTimelineOrder, PSOptionsOrderItems);
        psKeyframeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframePixels, 1, 50);
        psOptionsOverlapBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Timeline.OverlapCheck"), lsOptionsDraft.LPreferenceOverlapAllowed);
        psWaveformBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Timeline.WaveformCheck"), lsOptionsDraft.LPreferenceWaveform);

        psOptionsParallelSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceParallelMaximum, 1, 8);
        psOptionsFailureBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Work.FailurePause"), lsOptionsDraft.LPreferenceFailurePaused);
        psOptionsRetryBox = PSOptionsCheckBuild(LLocalization.LLocalizationTextRead("Options.Work.RetryCheck"), lsOptionsDraft.LPreferenceRetryAllowed);
        psOptionsRetrySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceRetryMaximum, 0, 10);

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
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Language"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.Language"), psOptionsLanguageCombo)));
        return pPanel;
    }

    private UIElement PSOptionsStartupBuild()
    {
        psOptionsTabPicker.Margin = new Thickness(0, 9, 0, 0);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, MinHeight = PSFieldControlHeight });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        TextBlock pLabel = PSFieldLabelBuild(LLocalization.LLocalizationTextRead("Options.General.OpenWith"));
        Grid.SetRow(pLabel, 0);
        pGrid.Children.Add(pLabel);

        Grid.SetRow(psOptionsStartupSession, 0);
        Grid.SetColumn(psOptionsStartupSession, 1);
        pGrid.Children.Add(psOptionsStartupSession);

        Grid.SetRow(psOptionsStartupDefault, 0);
        Grid.SetColumn(psOptionsStartupDefault, 2);
        pGrid.Children.Add(psOptionsStartupDefault);

        Grid.SetRow(psOptionsTabPicker, 1);
        Grid.SetColumn(psOptionsTabPicker, 2);
        pGrid.Children.Add(psOptionsTabPicker);
        return pGrid;
    }

    private UIElement PSPlaybackBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Autoplay"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.Autoplay"), psOptionsAutoplayBox)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.VolumePlate"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.VolumeMode"), psOptionsVolumeCombo),
            PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.DefaultVolume"), psOptionsVolumeSlider, "%")));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Mousewheel"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.OverTimeline"), psOptionsWheelCombo)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Playback.Dragging"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Playback.WhileDragging"), psOptionsDragBox)));
        return pPanel;
    }

    private UIElement PSWorkBuild()
    {
        UIElement pRetryRow = PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.RetryLimit"), psOptionsRetrySlider, string.Empty);
        pRetryRow.IsEnabled = psOptionsRetryBox.IsChecked == true;
        psOptionsRetryBox.Checked += (_, _) => pRetryRow.IsEnabled = true;
        psOptionsRetryBox.Unchecked += (_, _) => pRetryRow.IsEnabled = false;

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Work.MaxParallel"),
            PSOptionsFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.JobsAtOnce"), psOptionsParallelSlider, string.Empty)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Work.Failure"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.OnFailure"), psOptionsFailureBox),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.Retry"), psOptionsRetryBox),
            pRetryRow));
        return pPanel;
    }

    private void PSOptionsApply()
    {
        lsOptionsDraft.LPreferenceStartupMode = psOptionsStartupDefault.IsChecked == true ? "DefaultTab" : "LastSession";
        lsOptionsDraft.LPreferenceRecordWorkspace = psOptionsRecordWorkspace.IsChecked == true;
        lsOptionsDraft.LPreferenceStartupTabs = psOptionsTabPicker.PPickerSelectionRead().ToList();
        lsOptionsDraft.LPreferenceMediaAutomatic = psMediaBox.IsChecked == true;
        lsOptionsDraft.LPreferenceConfirmDestructive = psOptionsConfirmBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRelayEmpty = psRelayClearBox.IsChecked == true;
        lsOptionsDraft.LPreferenceLanguage = PSComboTextRead(psOptionsLanguageCombo);

        lsOptionsDraft.LPreferenceAutoplay = psOptionsAutoplayBox.IsChecked == true;
        lsOptionsDraft.LPreferenceVolumeMode = PSComboTextRead(psOptionsVolumeCombo);
        lsOptionsDraft.LPreferenceVolume = psOptionsVolumeSlider.Value;
        lsOptionsDraft.LPreferenceWheelAction = PSComboTextRead(psOptionsWheelCombo);
        lsOptionsDraft.LPreferenceDragPaused = psOptionsDragBox.IsChecked == true;

        lsOptionsDraft.LPreferenceTimelineOrder = PSComboTextRead(psOptionsOrderCombo);
        lsOptionsDraft.LPreferenceKeyframePixels = psKeyframeSlider.Value;
        lsOptionsDraft.LPreferenceSectionPalette = psSpectrumName;
        lsOptionsDraft.LPreferenceOverlapAllowed = psOptionsOverlapBox.IsChecked == true;
        lsOptionsDraft.LPreferenceWaveform = psWaveformBox.IsChecked == true;

        lsOptionsDraft.LPreferenceParallelMaximum = psOptionsParallelSlider.Value;
        lsOptionsDraft.LPreferenceFailurePaused = psOptionsFailureBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRetryAllowed = psOptionsRetryBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRetryMaximum = psOptionsRetrySlider.Value;

        lsOptionsDraft.LPreferenceWorkspaceFolder = psWorkspaceBox.Text.Trim();
        lsOptionsDraft.LPreferenceFfmpegFolder = psSystemFfmpegBox.Text.Trim();

        lsOptionsDraft.LPreferenceNormalize();
        string psLanguagePrevious = LPreference.LPreferenceStateCurrent.LPreferenceLanguage;
        LPreference.LPreferenceStateSet(lsOptionsDraft.LPreferenceClone());
        psOptionsCallback?.Invoke(LPreference.LPreferenceStateCurrent);
        if (!string.Equals(psLanguagePrevious, LPreference.LPreferenceStateCurrent.LPreferenceLanguage, StringComparison.Ordinal))
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

    private static RadioButton PSOptionsRadioBuild(string pLabel, bool pChecked, string pGroupName = "PSOptionsStartup")
    {
        var pRadio = new RadioButton
        {
            GroupName = pGroupName,
            Content = pLabel,
            IsChecked = pChecked,
            Margin = new Thickness(0, 0, 24, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        PRadio.PRadioApply(pRadio);
        return pRadio;
    }

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
