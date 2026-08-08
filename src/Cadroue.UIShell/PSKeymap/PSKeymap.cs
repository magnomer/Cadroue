using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.MigrationInterface;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed class PSKeymap : Window
{
    private const int PSKeymapDwmPreference = 33;
    private const int PSKeymapDwmRound = 2;
    private const int PSKeymapDwmCaption = 35;
    private const int PSKeymapColor = 0x00F7E8DC;

    internal const string PSKeymapPlacementKey = "Shortcut";

    private const double PSKeymapWidthDefault = 720;
    private const double PSKeymapHeightDefault = 640;
    private const double PSKeymapWidthMinimum = 620;
    private const double PSKeymapHeightMinimum = 480;

    private const double PSSheetTabWidth = 112;
    private const int PSSheetTabCount = 4;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetGlobalIcon = "/PAssets/PTabs/PSSheetGeneral.svg";
    private const string PSSheetTabIcon = "/PAssets/PTabs/PSSheetSystem.svg";
    private const string PSSheetFlowIcon = "/PAssets/PTabs/PSSheetTimeline.svg";
    private const string PSSheetSplitIcon = "/PAssets/PTabs/PSplitButton.svg";

    private readonly List<LBindingRecord> lsKeymapDraft;
    private readonly Action<LPreferenceState>? psKeymapCallback;
    private readonly PSGrabber psKeymapGrabber;
    private readonly Dictionary<string, PSKeymapChord> psKeymapChords = new(StringComparer.Ordinal);

    internal static void PSKeymapShow(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        var psKeymap = new PSKeymap(pOwner, pApplyCallback);
        psKeymap.ShowDialog();
    }

    private PSKeymap(Window pOwner, Action<LPreferenceState>? pApplyCallback)
    {
        lsKeymapDraft = Cadroue.MigrationInterface.LBinding.LBindingNormalize(Cadroue.MigrationInterface.LBinding.LBindingCurrent);
        psKeymapCallback = pApplyCallback;

        foreach (LBindingCommand pCommand in Cadroue.MigrationInterface.LBinding.LBindingCatalogRead())
        {
            psKeymapChords[pCommand.LBindingCommandToken] = new PSKeymapChord(
                Cadroue.MigrationInterface.LBinding.LBindingGestureRead(lsKeymapDraft, pCommand.LBindingCommandToken),
                PSKeymapConflictClear);
        }

        Title = LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Title");
        Owner = pOwner;
        Width = PSKeymapWidthDefault;
        Height = PSKeymapHeightDefault;
        MinWidth = PSKeymapWidthMinimum;
        MinHeight = PSKeymapHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);
        Content = PSKeymapBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSKeymapPlacementKey);
        psKeymapGrabber = new PSGrabber(this);
        psKeymapGrabber.PSGrabberAttach();
        Closed += PSKeymapCloseHandle;
    }

    private UIElement PSKeymapBuild()
    {
        var pRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };
        pRoot.Children.Add(PSSheet.PSSheetControlBuild(
            PSSheetTabWidth,
            PSKeymapSheetBuild(Cadroue.MigrationInterface.LBinding.LBindingScopeGlobal, PSSheetGlobalIcon),
            PSKeymapSheetBuild(Cadroue.MigrationInterface.LBinding.LBindingScopeTab, PSSheetTabIcon),
            PSKeymapSheetBuild(Cadroue.MigrationInterface.LBinding.LBindingScopeFlow, PSSheetFlowIcon),
            PSKeymapSheetBuild(Cadroue.MigrationInterface.LBinding.LBindingScopeSplit, PSSheetSplitIcon)));
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(this, PSSheetStripWidth));
        return pRoot;
    }

    private TabItem PSKeymapSheetBuild(string pScope, string pIconPath)
    {
        string pScopeTitle = LLocalization.LLocalizationTextRead(Cadroue.MigrationInterface.LBinding.LBindingLabelRead(pScope));
        return PSSheet.PSSheetBuild(
            pScopeTitle,
            pIconPath,
            PSKeymapRootBuild(PSSheet.PSSheetScrollBuild(PSKeymapScopeBuild(pScope, pScopeTitle))));
    }

    private UIElement PSKeymapScopeBuild(string pScope, string pScopeTitle)
    {
        var pRows = new List<UIElement>();
        foreach (LBindingCommand pCommand in Cadroue.MigrationInterface.LBinding.LBindingScopeRead(pScope))
        {
            pRows.Add(PSKeymapRowBuild(pCommand));
        }

        pRows.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Notice"),
            Foreground = PSFieldMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(pScopeTitle, pRows.ToArray()));
        return pPanel;
    }

    private UIElement PSKeymapRowBuild(LBindingCommand pCommand)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pCommand.LBindingCommandKey),
            Foreground = PSFieldText,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 12, 0)
        };
        pGrid.Children.Add(pLabel);

        PSKeymapChord pChord = psKeymapChords[pCommand.LBindingCommandToken];
        Grid.SetColumn(pChord, 1);
        pGrid.Children.Add(pChord);
        return pGrid;
    }

    private UIElement PSKeymapRootBuild(UIElement pSheetContent)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new DockPanel { Margin = new Thickness(12) };

        Button pReset = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Chrome.Shortcuts.Reset"));
        pReset.Width = 140;
        pReset.Click += (_, _) => PSKeymapDefaultApply();
        DockPanel.SetDock(pReset, Dock.Left);
        pFooter.Children.Add(pReset);

        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        Button pApply = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Options.Button.Apply"));
        Button pOk = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Options.Button.OK"));
        Button pCancel = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Options.Button.Cancel"));
        pApply.Click += (_, _) => PSKeymapApply();
        pOk.Click += (_, _) => { PSKeymapApply(); Close(); };
        pCancel.Click += (_, _) => Close();
        pButtons.Children.Add(pApply);
        pButtons.Children.Add(pOk);
        pButtons.Children.Add(pCancel);
        pFooter.Children.Add(pButtons);

        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);
        pRoot.Children.Add(new DockPanel { Margin = new Thickness(18), Children = { pSheetContent } });
        return pRoot;
    }

    private void PSKeymapConflictClear(PSKeymapChord pSource, string pGesture)
    {
        foreach (PSKeymapChord pChord in psKeymapChords.Values)
        {
            if (!ReferenceEquals(pChord, pSource)
                && string.Equals(pChord.PSKeymapChordGesture, pGesture, StringComparison.OrdinalIgnoreCase))
            {
                pChord.PSKeymapChordSet(string.Empty);
            }
        }
    }

    private void PSKeymapDefaultApply()
    {
        foreach (KeyValuePair<string, PSKeymapChord> pEntry in psKeymapChords)
        {
            pEntry.Value.PSKeymapChordSet(Cadroue.MigrationInterface.LBinding.LBindingDefaultRead(pEntry.Key));
        }
    }

    private void PSKeymapApply()
    {
        List<LBindingRecord> psKeymapApplied = psKeymapChords
            .Select(pEntry => new LBindingRecord
            {
                LBindingRecordToken = pEntry.Key,
                LBindingRecordGesture = pEntry.Value.PSKeymapChordGesture
            })
            .ToList();

        Cadroue.MigrationInterface.LBinding.LBindingSet(psKeymapApplied);
        psKeymapCallback?.Invoke(LPreference.LPreferenceStateCurrent);
    }

    private void PSKeymapCloseHandle(object? sender, EventArgs e)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSKeymapPlacementKey);
        psKeymapGrabber.PSGrabberDetach();
        Closed -= PSKeymapCloseHandle;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PSKeymapDwmApply();
    }

    private void PSKeymapDwmApply()
    {
        IntPtr psKeymapHandle = new WindowInteropHelper(this).Handle;
        if (psKeymapHandle == IntPtr.Zero)
        {
            return;
        }

        int psKeymapCornerPreference = PSKeymapDwmRound;
        _ = DwmSetWindowAttribute(psKeymapHandle, PSKeymapDwmPreference, ref psKeymapCornerPreference, Marshal.SizeOf<int>());

        int psKeymapCaptionColor = PSKeymapColor;
        _ = DwmSetWindowAttribute(psKeymapHandle, PSKeymapDwmCaption, ref psKeymapCaptionColor, Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);
}
