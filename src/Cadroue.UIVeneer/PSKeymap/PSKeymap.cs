using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSKeymap : Window
{
    internal const string PSKeymapPlacementKey = "Shortcut";

    private const double PSKeymapWidthDefault = 720;
    private const double PSKeymapHeightDefault = 640;
    private const double PSKeymapWidthMinimum = 620;
    private const double PSKeymapHeightMinimum = 480;

    private const double PSSheetTabWidth = 112;
    private const int PSSheetTabCount = 4;
    private const double PSSheetStripWidth = PSSheetTabWidth * PSSheetTabCount;

    private const string PSSheetGlobalIcon = "/PAsset/PTab/PSSheetGeneral.svg";
    private const string PSSheetTabIcon = "/PAsset/PTab/PSSheetSystem.svg";
    private const string PSSheetFlowIcon = "/PAsset/PTab/PSSheetTimeline.svg";
    private const string PSSheetSplitIcon = "/PAsset/PTab/PSplitButton.svg";

    private readonly QSKeymap lsKeymap;
    private readonly Action? psKeymapCallback;
    private readonly PSGrabber psKeymapGrabber;
    private readonly Dictionary<string, PSKeymapChord> psKeymapChords = new(StringComparer.Ordinal);

    internal static void PSKeymapShow(Window pOwner, QSKeymap lsKeymapOwner, Action? pApplyCallback)
    {
        var psKeymap = new PSKeymap(pOwner, lsKeymapOwner, pApplyCallback);
        psKeymap.ShowDialog();
    }

    private PSKeymap(Window pOwner, QSKeymap lsKeymapOwner, Action? pApplyCallback)
    {
        lsKeymap = lsKeymapOwner;
        psKeymapCallback = pApplyCallback;
        PSKeymapButtonBuild();
        PSKeymapWindowApply(pOwner);
        Content = PSKeymapBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSKeymapPlacementKey);
        psKeymapGrabber = new PSGrabber(this);
        psKeymapGrabber.PSGrabberAttach();
        PSKeymapWindowAttach();
    }

    private void PSKeymapButtonBuild()
    {
        foreach (QSKeymapChord pChord in lsKeymap.QSKeymapChords)
        {
            psKeymapChords[pChord.QSKeymapChordToken] = new PSKeymapChord(lsKeymap, pChord);
        }
    }

    private void PSKeymapWindowApply(Window pOwner)
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Chrome.Shortcuts.Title");
        Owner = pOwner;
        Width = PSKeymapWidthDefault;
        Height = PSKeymapHeightDefault;
        MinWidth = PSKeymapWidthMinimum;
        MinHeight = PSKeymapHeightMinimum;
        PSSubwindow.PSSubwindowApply(this);
    }

    private void PSKeymapWindowAttach() => Closed += PSKeymapCloseHandle;

    private void PSKeymapWindowDetach() => Closed -= PSKeymapCloseHandle;

    private UIElement PSKeymapBuild() =>
        PSSubwindow.PSSubwindowBuild(this, PSSheetStripWidth, PSSheet.PSSheetControlBuild(
            PSSheetTabWidth,
            PSKeymapSheetBuild(Cadroue.Infrastructure.LBinding.LBindingScopeGlobal, PSSheetGlobalIcon),
            PSKeymapSheetBuild(Cadroue.Infrastructure.LBinding.LBindingScopeTab, PSSheetTabIcon),
            PSKeymapSheetBuild(Cadroue.Infrastructure.LBinding.LBindingScopeFlow, PSSheetFlowIcon),
            PSKeymapSheetBuild(Cadroue.Infrastructure.LBinding.LBindingScopeSplit, PSSheetSplitIcon)));

    private TabItem PSKeymapSheetBuild(string pScope, string pIconPath)
    {
        string pScopeTitle = QLocalizationCatalog.QLocalizationTextRead(
            Cadroue.Infrastructure.LBinding.LBindingLabelRead(pScope));
        return PSSheet.PSSheetBuild(
            pScopeTitle,
            pIconPath,
            PSKeymapRootBuild(PSSheet.PSSheetScrollBuild(PSKeymapScopeBuild(pScope, pScopeTitle))));
    }

    private UIElement PSKeymapScopeBuild(string pScope, string pScopeTitle)
    {
        var pRows = new List<UIElement>();
        foreach (LBindingCommand pCommand in Cadroue.Infrastructure.LBinding.LBindingScopeRead(pScope))
        {
            pRows.Add(PSKeymapRowBuild(pCommand));
        }

        pRows.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Chrome.Shortcuts.Notice"),
            Foreground = PSField.PSFieldMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlate.PSPlateBuild(pScopeTitle, pRows.ToArray()));
        return pPanel;
    }

    private UIElement PSKeymapRowBuild(LBindingCommand pCommand)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead(pCommand.LBindingCommandKey),
            Foreground = PSField.PSFieldText,
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
        Button pReset = PSKeymapResetBuild();
        Button pApply = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Button.Apply"));
        Button pOk = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Button.OK"));
        Button pCancel = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Options.Button.Cancel"));
        PSKeymapFooterAttach(pReset, pApply, pOk, pCancel);
        return PSKeymapFrameBuild(pSheetContent, pReset, pApply, pOk, pCancel);
    }

    private static Button PSKeymapResetBuild()
    {
        Button pReset = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Chrome.Shortcuts.Reset"));
        pReset.Width = 140;
        DockPanel.SetDock(pReset, Dock.Left);
        return pReset;
    }

    private void PSKeymapFooterAttach(Button pReset, Button pApply, Button pOk, Button pCancel)
    {
        pReset.Click += (_, _) => PSKeymapDefaultApply();
        pApply.Click += (_, _) => PSKeymapApply();
        pOk.Click += (_, _) => PSKeymapConfirm();
        pCancel.Click += (_, _) => Close();
    }

    private void PSKeymapConfirm()
    {
        PSKeymapApply();
        Close();
    }

    private static DockPanel PSKeymapFrameBuild(
        UIElement pSheetContent,
        Button pReset,
        Button pApply,
        Button pOk,
        Button pCancel)
    {
        var pRoot = new DockPanel { Background = Brushes.White };
        var pFooter = new DockPanel { Margin = new Thickness(12) };
        pFooter.Children.Add(pReset);

        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pButtons.Children.Add(pApply);
        pButtons.Children.Add(pOk);
        pButtons.Children.Add(pCancel);
        pFooter.Children.Add(pButtons);

        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);
        pRoot.Children.Add(new DockPanel { Margin = new Thickness(18), Children = { pSheetContent } });
        return pRoot;
    }

    private void PSKeymapDefaultApply() => lsKeymap.QSKeymapDefaultApply();

    private void PSKeymapApply()
    {
        lsKeymap.QSKeymapApply();
        psKeymapCallback?.Invoke();
    }

    private void PSKeymapCloseHandle(object? sender, EventArgs e)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSKeymapPlacementKey);
        psKeymapGrabber.PSGrabberDetach();
        PSKeymapWindowDetach();
    }
}
