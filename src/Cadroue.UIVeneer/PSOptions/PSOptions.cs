using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSOptions : Window
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
    private readonly Action? psOptionsCallback;
    private readonly PSOptionsDraft psOptionsDraft;
    private readonly PSOptionsSystem psOptionsSystem;
    private readonly PSGrabber psOptionsGrabber;

    internal static void PSOptionsShow(Window pOwner, QSOptions lsOptionsOwner, Action? pApplyCallback)
    {
        var psOptions = new PSOptions(pOwner, lsOptionsOwner, pApplyCallback);
        psOptions.ShowDialog();
    }

    private PSOptions(Window pOwner, QSOptions lsOptionsOwner, Action? pApplyCallback)
    {
        lsOptions = lsOptionsOwner;
        psOptionsCallback = pApplyCallback;
        psOptionsDraft = new PSOptionsDraft(
            lsOptions, new PSOptionsSpectrum(lsOptions.QSOptionsSpectrumCreate(), this));
        psOptionsSystem = new PSOptionsSystem(lsOptions, this);

        PSOptionsWindowApply(pOwner);
        Content = PSOptionsBuild();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSOptionsPlacementKey);
        psOptionsGrabber = new PSGrabber(this);
        psOptionsGrabber.PSGrabberAttach();
        PSOptionsWindowAttach();
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
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(psOptionsDraft.psOptionsGeneral.PSGeneralBuild()))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.System"),
                PSSheetSystemIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(psOptionsSystem.PSSystemBuild(lsOptions, this)))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Playback"),
                PSSheetPlaybackIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(psOptionsDraft.psOptionsPlayback.PSPlaybackBuild(
                    new PSOptionsSystemFlyleaf(lsOptions, this), new PSOptionsSystemMpv(lsOptions, this))))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Timeline"),
                PSSheetTimelineIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(psOptionsDraft.psOptionsTimeline.PSTimelineBuild()))),
            PSSheet.PSSheetBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Work"),
                PSSheetWorkIcon,
                PSOptionsRootBuild(PSSheet.PSSheetScrollBuild(psOptionsDraft.psOptionsWork.PSWorkBuild(lsOptions)))));
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
        psOptionsDraft.PSOptionsDraftUpdate(psOptionsSystem, this);
        bool psOptionsSaved = lsOptions.QSOptionsApply();
        psOptionsCallback?.Invoke();
        psOptionsSystem.PSSystemAppliedUpdate();
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

    private void PSOptionsCloseHandle(object? sender, EventArgs e)
    {
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSOptionsPlacementKey);
        psOptionsGrabber.PSGrabberDetach();
        PSOptionsWindowDetach();
    }
}
