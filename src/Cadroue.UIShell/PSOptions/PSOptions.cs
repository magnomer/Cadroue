using System.Runtime.InteropServices;
using System.Windows;
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

    internal const double PSOptionsWidthDefault = 900;
    internal const double PSOptionsHeightDefault = 660;
    internal const double PSOptionsWidthMinimum = 780;
    internal const double PSOptionsHeightMinimum = 520;

    private const double PSSheetTabWidth = 112;
    private const int PSSheetTabCount = 5;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetGeneralIconPath = "/PAssets/PTabs/PSSheetGeneral.svg";
    private const string PSSheetSystemIconPath = "/PAssets/PTabs/PSSheetSystem.svg";
    private const string PSSheetPlaybackIconPath = "/PAssets/PTabs/PSSheetPlayback.svg";
    private const string PSSheetTimelineIconPath = "/PAssets/PTabs/PSSheetTimeline.svg";
    private const string PSSheetWorkIconPath = "/PAssets/PTabs/PSSheetWork.svg";

    private static readonly string[] PSOptionsTabKeys = { "Split", "Edit", "Audio", "Convert", "Merge", "Worklist" };
    private static readonly string[] PSOptionsVolumeItems = { "Per-tab volume", "Unified volume" };
    private static readonly string[] PSOptionsVolumeTokens = { "PerTab", "Unified" };
    private static readonly string[] PSOptionsOrderItems = { "Map on top", "Viewfinder on top" };
    private static readonly string[] PSOptionsOrderTokens = { "MapFirst", "ViewfinderFirst" };

    private readonly LPreferenceState lsOptionsDraft;
    private readonly Action<LPreferenceState>? psOptionsCallback;
    private readonly PSGrabber psOptionsGrabber;

    private readonly RadioButton psStartupSession;
    private readonly RadioButton psStartupDefault;
    private readonly PPicker psStartupTabPicker;
    private readonly CheckBox psMediaBox;
    private readonly CheckBox psConfirmBox;
    private readonly ComboBox psLanguageCombo;

    private readonly CheckBox psAutoplayBox;
    private readonly ComboBox psVolumeModeCombo;
    private readonly Slider psVolumeSlider;
    private readonly ComboBox psWheelCombo;
    private readonly CheckBox psDragBox;

    private readonly ComboBox psOrderCombo;
    private readonly Slider psKeyframeSlider;
    private readonly CheckBox psOverlapBox;

    private readonly Slider psParallelSlider;
    private readonly CheckBox psFailureBox;
    private readonly CheckBox psRetryBox;
    private readonly Slider psRetrySlider;

    internal static void PSOptionsShow(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        var psOptions = new PSOptions(pOwner, pApplyCallback);
        psOptions.ShowDialog();
    }

    private PSOptions(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        lsOptionsDraft = App.LPreferenceStateCurrent.LPreferenceClone();
        psOptionsCallback = pApplyCallback;

        psStartupSession = PSOptionsRadioBuild("Last session", lsOptionsDraft.LPreferenceStartupMode == "LastSession");
        psStartupDefault = PSOptionsRadioBuild("Default tab", lsOptionsDraft.LPreferenceStartupMode == "DefaultTab");
        psStartupTabPicker = new PPicker(PSOptionsTabKeys, lsOptionsDraft.LPreferenceStartupTabs, "No tab chosen")
        {
            MinWidth = 260,
            Height = PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        psMediaBox = PSOptionsCheckBuild("Open the last media on startup", lsOptionsDraft.LPreferenceMediaAutomatic);
        psConfirmBox = PSOptionsCheckBuild("Ask before a destructive action", lsOptionsDraft.LPreferenceConfirmDestructive);
        psLanguageCombo = PSComboBuild(lsOptionsDraft.LPreferenceLanguage, "English");

        psAutoplayBox = PSOptionsCheckBuild("Start playing as soon as media loads", lsOptionsDraft.LPreferenceAutoplayOnLoad);
        psVolumeModeCombo = PSComboBuild(
            PSOptionsLabelRead(lsOptionsDraft.LPreferenceVolumeMode, PSOptionsVolumeTokens, PSOptionsVolumeItems),
            PSOptionsVolumeItems);
        psVolumeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceVolume, 0, 100);
        psWheelCombo = PSComboBuild(lsOptionsDraft.LPreferenceWheelAction, "Seek", "Zoom", "Volume");
        psDragBox = PSOptionsCheckBuild("Pause while dragging the timeline", lsOptionsDraft.LPreferenceDragPaused);

        psOrderCombo = PSComboBuild(
            PSOptionsLabelRead(lsOptionsDraft.LPreferenceTimelineOrder, PSOptionsOrderTokens, PSOptionsOrderItems),
            PSOptionsOrderItems);
        psKeyframeSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceKeyframeMinimumPixels, 1, 50);
        psOverlapBox = PSOptionsCheckBuild("Allow sections to overlap", lsOptionsDraft.LPreferenceOverlapAllowed);

        psParallelSlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceParallelMaximum, 1, 8);
        psFailureBox = PSOptionsCheckBuild("Pause the queue when a job fails", lsOptionsDraft.LPreferenceFailurePaused);
        psRetryBox = PSOptionsCheckBuild("Retry a failed job", lsOptionsDraft.LPreferenceRetryAllowed);
        psRetrySlider = PSOptionsSliderBuild(lsOptionsDraft.LPreferenceRetryMaximum, 0, 10);

        psWorkspaceBox = PSEntryBuild(lsOptionsDraft.LPreferenceWorkspaceFolder, 320);
        psFfmpegBox = PSEntryBuild(lsOptionsDraft.LPreferenceFfmpegFolder, 320);

        Title = "Options";
        Owner = pOwner;
        Width = App.LPreferenceStateCurrent.LPreferenceOptionsWidth;
        Height = App.LPreferenceStateCurrent.LPreferenceOptionsHeight;
        MinWidth = PSOptionsWidthMinimum;
        MinHeight = PSOptionsHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldBodyFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);
        Content = PSOptionsBuild();
        PSOptionsPositionRestore(App.LPreferenceStateCurrent);
        psOptionsGrabber = new PSGrabber(this);
        psOptionsGrabber.PSGrabberAttach();
        Closed += PSOptionsCloseHandle;
    }

    private UIElement PSOptionsBuild()
    {
        var pRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };
        pRoot.Children.Add(PSSheet.PSSheetControlBuild(
            PSSheetTabWidth,
            PSSheet.PSSheetBuild("General", PSSheetGeneralIconPath, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSGeneralBuild()))),
            PSSheet.PSSheetBuild("System", PSSheetSystemIconPath, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSSystemBuild()))),
            PSSheet.PSSheetBuild("Playback", PSSheetPlaybackIconPath, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSPlaybackBuild()))),
            PSSheet.PSSheetBuild("Timeline", PSSheetTimelineIconPath, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSTimelineBuild()))),
            PSSheet.PSSheetBuild("Work", PSSheetWorkIconPath, PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(PSWorkBuild())))));
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(this, PSSheetStripWidth));
        return pRoot;
    }

    private UIElement PSOptionsRootBuild(UIElement pSheetContent)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12) };
        Button pApply = PSFooterButtonBuild("Apply");
        Button pOk = PSFooterButtonBuild("OK");
        Button pCancel = PSFooterButtonBuild("Cancel");
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
        pPanel.Children.Add(PSPlateBuild("Startup",
            PSOptionsStartupBuild(),
            PSFieldBuild("Last media", psMediaBox)));
        pPanel.Children.Add(PSPlateBuild("Confirm",
            PSFieldBuild("Destructive actions", psConfirmBox),
            PSNoticeBuild("Clearing sections, clearing done jobs, and removing all queued items.")));
        pPanel.Children.Add(PSPlateBuild("Language",
            PSFieldBuild("Language", psLanguageCombo)));
        return pPanel;
    }

    private UIElement PSOptionsStartupBuild()
    {
        psStartupTabPicker.Margin = new Thickness(0, 9, 0, 0);

        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        TextBlock pLabel = PSFieldLabelBuild("Open with");
        Grid.SetRow(pLabel, 0);
        pGrid.Children.Add(pLabel);

        Grid.SetRow(psStartupSession, 0);
        Grid.SetColumn(psStartupSession, 1);
        pGrid.Children.Add(psStartupSession);

        Grid.SetRow(psStartupDefault, 0);
        Grid.SetColumn(psStartupDefault, 2);
        pGrid.Children.Add(psStartupDefault);

        Grid.SetRow(psStartupTabPicker, 1);
        Grid.SetColumn(psStartupTabPicker, 2);
        pGrid.Children.Add(psStartupTabPicker);
        return pGrid;
    }

    private UIElement PSPlaybackBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild("Autoplay",
            PSFieldBuild("Autoplay", psAutoplayBox)));
        pPanel.Children.Add(PSPlateBuild("Volume",
            PSFieldBuild("Volume mode", psVolumeModeCombo),
            PSOptionsSliderFieldBuild("Default volume", psVolumeSlider, "%")));
        pPanel.Children.Add(PSPlateBuild("Mousewheel",
            PSFieldBuild("Over the timeline", psWheelCombo)));
        pPanel.Children.Add(PSPlateBuild("Dragging",
            PSFieldBuild("While dragging", psDragBox)));
        return pPanel;
    }

    private UIElement PSWorkBuild()
    {
        UIElement pRetryRow = PSOptionsSliderFieldBuild("Retry limit", psRetrySlider, string.Empty);
        pRetryRow.IsEnabled = psRetryBox.IsChecked == true;
        psRetryBox.Checked += (_, _) => pRetryRow.IsEnabled = true;
        psRetryBox.Unchecked += (_, _) => pRetryRow.IsEnabled = false;

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild("Max parallel jobs",
            PSOptionsSliderFieldBuild("Jobs at once", psParallelSlider, string.Empty)));
        pPanel.Children.Add(PSPlateBuild("Failure",
            PSFieldBuild("On failure", psFailureBox),
            PSFieldBuild("Retry", psRetryBox),
            pRetryRow));
        return pPanel;
    }

    private void PSOptionsApply()
    {
        lsOptionsDraft.LPreferenceStartupMode = psStartupDefault.IsChecked == true ? "DefaultTab" : "LastSession";
        lsOptionsDraft.LPreferenceStartupTabs = psStartupTabPicker.PPickerSelectionRead().ToList();
        lsOptionsDraft.LPreferenceMediaAutomatic = psMediaBox.IsChecked == true;
        lsOptionsDraft.LPreferenceConfirmDestructive = psConfirmBox.IsChecked == true;
        lsOptionsDraft.LPreferenceLanguage = PSComboTextRead(psLanguageCombo);

        lsOptionsDraft.LPreferenceAutoplayOnLoad = psAutoplayBox.IsChecked == true;
        lsOptionsDraft.LPreferenceVolumeMode = PSOptionsTokenRead(
            PSComboTextRead(psVolumeModeCombo), PSOptionsVolumeItems, PSOptionsVolumeTokens);
        lsOptionsDraft.LPreferenceVolume = psVolumeSlider.Value;
        lsOptionsDraft.LPreferenceWheelAction = PSComboTextRead(psWheelCombo);
        lsOptionsDraft.LPreferenceDragPaused = psDragBox.IsChecked == true;

        lsOptionsDraft.LPreferenceTimelineOrder = PSOptionsTokenRead(
            PSComboTextRead(psOrderCombo), PSOptionsOrderItems, PSOptionsOrderTokens);
        lsOptionsDraft.LPreferenceKeyframeMinimumPixels = psKeyframeSlider.Value;
        lsOptionsDraft.LPreferenceSectionPalette = psPaletteName;
        lsOptionsDraft.LPreferenceOverlapAllowed = psOverlapBox.IsChecked == true;

        lsOptionsDraft.LPreferenceParallelMaximum = psParallelSlider.Value;
        lsOptionsDraft.LPreferenceFailurePaused = psFailureBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRetryAllowed = psRetryBox.IsChecked == true;
        lsOptionsDraft.LPreferenceRetryMaximum = psRetrySlider.Value;

        lsOptionsDraft.LPreferenceWorkspaceFolder = psWorkspaceBox.Text.Trim();
        lsOptionsDraft.LPreferenceFfmpegFolder = psFfmpegBox.Text.Trim();

        lsOptionsDraft.LPreferenceNormalize();
        App.LPreferenceStateSet(lsOptionsDraft.LPreferenceClone());
        psOptionsCallback?.Invoke(App.LPreferenceStateCurrent);
    }

    private static RadioButton PSOptionsRadioBuild(string pLabel, bool pChecked)
    {
        var pRadio = new RadioButton
        {
            GroupName = "PSOptionsStartup",
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

    private static UIElement PSOptionsSliderFieldBuild(string pLabel, Slider pSlider, string pUnit)
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

    private static string PSOptionsLabelRead(string pToken, string[] pTokens, string[] pLabels)
    {
        int pIndex = Array.IndexOf(pTokens, pToken);
        return pIndex < 0 ? pLabels[0] : pLabels[pIndex];
    }

    private static string PSOptionsTokenRead(string pLabel, string[] pLabels, string[] pTokens)
    {
        int pIndex = Array.IndexOf(pLabels, pLabel);
        return pIndex < 0 ? pTokens[0] : pTokens[pIndex];
    }

    private static string PSOptionsNumberFormat(double pValue) => $"{Math.Round(pValue):0}";

    private void PSOptionsPositionRestore(LPreferenceState lPreferenceState)
    {
        if (lPreferenceState.LPreferenceOptionsLeft is not double psLeft
            || lPreferenceState.LPreferenceOptionsTop is not double psTop)
        {
            return;
        }

        double psScreenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        double psScreenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        if (psLeft >= SystemParameters.VirtualScreenLeft && psTop >= SystemParameters.VirtualScreenTop
            && psLeft + 100 <= psScreenRight && psTop + 40 <= psScreenBottom)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = psLeft;
            Top = psTop;
        }
    }

    private void PSOptionsCloseHandle(object? sender, EventArgs e)
    {
        PSOptionsPositionSave();
        psOptionsGrabber.PSGrabberDetach();
        Closed -= PSOptionsCloseHandle;
    }

    private void PSOptionsPositionSave()
    {
        Rect psBounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (double.IsNaN(psBounds.Left) || double.IsNaN(psBounds.Top) || psBounds.Width <= 0 || psBounds.Height <= 0)
        {
            return;
        }

        LPreferenceState lPreferenceState = App.LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceState.LPreferenceOptionsLeft = psBounds.Left;
        lPreferenceState.LPreferenceOptionsTop = psBounds.Top;
        lPreferenceState.LPreferenceOptionsWidth = psBounds.Width;
        lPreferenceState.LPreferenceOptionsHeight = psBounds.Height;
        App.LPreferenceStateSet(lPreferenceState);
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
