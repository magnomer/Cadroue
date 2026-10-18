using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSVerdict : Window
{
    private const string PSVerdictPlacementKey = "Verdict";

    private const double PSVerdictWidthDefault = 660;
    private const double PSVerdictWidthMinimum = 480;
    private const double PSVerdictHeightDefault = 560;
    private const double PSVerdictHeightMinimum = 360;
    private const double PSVerdictInset = 18;

    private static PSVerdict? psVerdictCurrent;

    private readonly QSVerdict lsVerdict;
    private readonly PSGrabber psVerdictGrabber;

    internal static void PSVerdictShow(Window pOwner, QSVerdict pVerdict)
    {
        psVerdictCurrent?.Close();
        var psVerdict = new PSVerdict(pOwner, pVerdict);
        PSVerdictCurrentSet(psVerdict);
        psVerdict.Show();
    }

    private static void PSVerdictCurrentSet(PSVerdict? psVerdict) => psVerdictCurrent = psVerdict;

    private PSVerdict(Window pOwner, QSVerdict pVerdict)
    {
        lsVerdict = pVerdict;
        PSVerdictWindowApply(pOwner, pVerdict);
        QScrollbar.QScrollbarAttach(this);
        Content = PSVerdictBuild();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSVerdictPlacementKey);
        psVerdictGrabber = new PSGrabber(this);
        psVerdictGrabber.PSGrabberAttach();
        PSVerdictWindowAttach();
    }

    private void PSVerdictWindowApply(Window pOwner, QSVerdict pVerdict)
    {
        Title = pVerdict.QSVerdictTitle;
        Owner = pOwner.Owner ?? pOwner;
        ShowInTaskbar = true;
        Width = PSVerdictWidthDefault;
        Height = PSVerdictHeightDefault;
        MinWidth = PSVerdictWidthMinimum;
        MinHeight = PSVerdictHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
    }

    private void PSVerdictWindowAttach() => Closed += PSVerdictCloseHandle;

    private void PSVerdictWindowDetach() => Closed -= PSVerdictCloseHandle;

    private UIElement PSVerdictBuild() =>
        PSDialog.PSDialogBuild(this, lsVerdict.QSVerdictTitle, PSVerdictRootBuild());

    private DockPanel PSVerdictRootBuild()
    {
        Button pClose = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Encoder.Verification.Close"));
        PSVerdictFooterAttach(pClose);
        return PSVerdictFrameBuild(pClose);
    }

    private void PSVerdictFooterAttach(Button pClose) => pClose.Click += (_, _) => Close();

    private DockPanel PSVerdictFrameBuild(Button pClose)
    {
        var pRoot = new DockPanel
        {
            Background = Brushes.White
        };

        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        pFooter.Children.Add(pClose);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);

        var pBody = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        pBody.Children.Add(PSVerdictTable.PSVerdictTableBuild(lsVerdict.QSVerdictRows));

        ScrollViewer pScroll = PSSheet.PSSheetScrollBuild(pBody);
        pScroll.Margin = new Thickness(PSVerdictInset, 0, PSVerdictInset, 8);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private void PSVerdictCloseHandle(object? pSender, EventArgs pEvent)
    {
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSVerdictPlacementKey);
        psVerdictGrabber.PSGrabberDetach();
        PSVerdictWindowDetach();
        if (ReferenceEquals(psVerdictCurrent, this))
        {
            PSVerdictCurrentSet(null);
        }
    }
}
