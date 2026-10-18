using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAbout : Window
{
    internal const string PSAboutPlacementKey = "About";

    private const double PSAboutWidthDefault = 460;
    private const double PSAboutWidthMinimum = 400;
    private const double PSAboutHeightMinimum = 400;

    private readonly QSAbout lsAbout;
    private readonly PSGrabber psAboutGrabber;

    internal static void PSAboutShow(Window pOwner, QSAbout lsAboutOwner)
    {
        var psAbout = new PSAbout(pOwner, lsAboutOwner);
        psAbout.ShowDialog();
    }

    private PSAbout(Window pOwner, QSAbout lsAboutOwner)
    {
        lsAbout = lsAboutOwner;
        PSAboutLookApply(pOwner);
        QScrollbar.QScrollbarAttach(this);
        lsAbout.QSAboutErrorShown += PSAboutErrorShow;
        PSAboutContentApply();
        PSAboutLoadAttach();
        psAboutGrabber = new PSGrabber(this);
        psAboutGrabber.PSGrabberAttach();
        PSAboutCloseAttach();
    }

    private void PSAboutLookApply(Window pOwner)
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("About.Window.Title");
        Owner = pOwner;
        Width = PSAboutWidthDefault;
        MinWidth = PSAboutWidthMinimum;
        MinHeight = PSAboutHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
    }

    private void PSAboutContentApply()
    {
        Content = PSAboutBuild();
        SizeToContent = SizeToContent.Height;
    }

    private void PSAboutLoadAttach() => Loaded += PSAboutLoadedHandle;

    private void PSAboutLoadDetach() => Loaded -= PSAboutLoadedHandle;

    private void PSAboutCloseAttach() => Closed += PSAboutCloseHandle;

    private void PSAboutCloseDetach() => Closed -= PSAboutCloseHandle;

    private void PSAboutLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSAboutLoadDetach();
        PSAboutHeightApply();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSAboutPlacementKey);
    }

    private void PSAboutHeightApply()
    {
        double pContentHeight = ActualHeight;
        SizeToContent = SizeToContent.Manual;
        Height = PSAboutHeightClamp(pContentHeight);
    }

    private double PSAboutHeightClamp(double pContentHeight) => Math.Max(pContentHeight, MinHeight);

    private UIElement PSAboutBuild() =>
        PSDialog.PSDialogBuild(this, null, PSAboutRootBuild());

    private DockPanel PSAboutRootBuild()
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
        Button pClose = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("About.Button.Close"));
        PSAboutButtonAttach(pClose);
        pFooter.Children.Add(pClose);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);

        var pBody = new StackPanel { Margin = new Thickness(18, 14, 18, 0) };
        pBody.Children.Add(new PSAboutHeader(lsAbout).PSAboutHeaderBuild());
        pBody.Children.Add(new PSAboutCredit(lsAbout).PSAboutCreditBuild());
        pRoot.Children.Add(PSSheet.PSSheetScrollBuild(pBody));
        return pRoot;
    }

    private void PSAboutButtonAttach(Button pClose) => pClose.Click += (_, _) => Close();

    private void PSAboutErrorShow(string pResource, string pDetail) =>
        PSWarning.PSWarningShow(
            this,
            QLocalizationCatalog.QLocalizationTextRead("About.Window.Title"),
            QLocalizationCatalog.QLocalizationFormat("About.Error.Open", pResource, pDetail));

    private void PSAboutCloseHandle(object? pSender, EventArgs pEvent)
    {
        PSAboutLoadDetach();
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSAboutPlacementKey);
        psAboutGrabber.PSGrabberDetach();
        PSAboutCloseDetach();
    }

}
