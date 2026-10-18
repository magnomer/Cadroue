using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSDiagnosis : Window
{
    internal const string PSDiagnosisPlacementKey = "Diagnosis";

    private const double PSDiagnosisWidthDefault = 560;
    private const double PSDiagnosisWidthMinimum = 460;
    private const double PSDiagnosisHeightDefault = 760;
    private const double PSDiagnosisHeightMinimum = 520;

    private static PSDiagnosis? psDiagnosisCurrent;

    private readonly QSDiagnosis lsDiagnosis;
    private readonly PSDiagnosisContent psDiagnosisContent;
    private readonly PSGrabber psDiagnosisGrabber;

    internal static void PSDiagnosisShow(Window pOwner, Func<QSDiagnosis> lsDiagnosisCreate)
    {
        if (psDiagnosisCurrent is not null)
        {
            psDiagnosisCurrent.Activate();
            return;
        }

        var psDiagnosis = new PSDiagnosis(pOwner, lsDiagnosisCreate());
        PSDiagnosisCurrentSet(psDiagnosis);
        psDiagnosis.Show();
    }

    private static void PSDiagnosisCurrentSet(PSDiagnosis? psDiagnosis) => psDiagnosisCurrent = psDiagnosis;

    private PSDiagnosis(Window pOwner, QSDiagnosis lsDiagnosisOwner)
    {
        lsDiagnosis = lsDiagnosisOwner;
        psDiagnosisContent = new PSDiagnosisContent();
        PSDiagnosisWindowApply(pOwner);
        QScrollbar.QScrollbarAttach(this);
        Content = PSDiagnosisBuild();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSDiagnosisPlacementKey);
        psDiagnosisGrabber = new PSGrabber(this);
        psDiagnosisGrabber.PSGrabberAttach();
        PSDiagnosisWindowAttach();
        PSDiagnosisResultAttach();
        _ = lsDiagnosis.QSDiagnosisProbeStart();
    }

    private void PSDiagnosisWindowApply(Window pOwner)
    {
        Title = lsDiagnosis.QSDiagnosisTitle;
        Owner = pOwner.Owner ?? pOwner;
        ShowInTaskbar = true;
        Width = PSDiagnosisWidthDefault;
        Height = PSDiagnosisHeightDefault;
        MinWidth = PSDiagnosisWidthMinimum;
        MinHeight = PSDiagnosisHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
    }

    private void PSDiagnosisWindowAttach() => Closed += PSDiagnosisCloseHandle;

    private void PSDiagnosisWindowDetach() => Closed -= PSDiagnosisCloseHandle;

    private void PSDiagnosisResultAttach() => lsDiagnosis.QSDiagnosisChanged += PSDiagnosisResultHandle;

    private void PSDiagnosisResultDetach() => lsDiagnosis.QSDiagnosisChanged -= PSDiagnosisResultHandle;

    private UIElement PSDiagnosisBuild() =>
        PSDialog.PSDialogBuild(this, lsDiagnosis.QSDiagnosisTitle, PSDiagnosisRootBuild());

    private DockPanel PSDiagnosisRootBuild()
    {
        Button pClose = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Diagnosis.Button.Close"));
        PSDiagnosisFooterAttach(pClose);
        return PSDiagnosisFrameBuild(pClose);
    }

    private void PSDiagnosisFooterAttach(Button pClose) => pClose.Click += (_, _) => Close();

    private DockPanel PSDiagnosisFrameBuild(Button pClose)
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

        UIElement pBanner = psDiagnosisContent.PSDiagnosisBannerBuild(lsDiagnosis);
        DockPanel.SetDock(pBanner, Dock.Top);
        pRoot.Children.Add(pBanner);

        ScrollViewer pScroll = PSSheet.PSSheetScrollBuild(psDiagnosisContent.PSDiagnosisContentBuild());
        pScroll.Margin = new Thickness(
            PSDiagnosisContent.PSDiagnosisInset, 0, PSDiagnosisContent.PSDiagnosisInset, 8);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private void PSDiagnosisResultHandle()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(PSDiagnosisResultHandle);
            return;
        }

        psDiagnosisContent.PSDiagnosisResultApply(lsDiagnosis);
    }

    private void PSDiagnosisCloseHandle(object? pSender, EventArgs pEvent)
    {
        lsDiagnosis.QSDiagnosisClose();
        PSDiagnosisResultDetach();
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSDiagnosisPlacementKey);
        psDiagnosisGrabber.PSGrabberDetach();
        PSDiagnosisWindowDetach();
        if (ReferenceEquals(psDiagnosisCurrent, this))
        {
            PSDiagnosisCurrentSet(null);
        }
    }
}
