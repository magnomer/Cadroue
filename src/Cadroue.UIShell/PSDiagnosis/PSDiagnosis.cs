using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSFooter;

namespace Cadroue.UIShell;

internal sealed partial class PSDiagnosis : Window
{
    internal const string PSDiagnosisPlacementKey = "Diagnosis";

    private const double PSDiagnosisWidthDefault = 560;
    private const double PSDiagnosisWidthMinimum = 460;
    private const double PSDiagnosisHeightDefault = 760;
    private const double PSDiagnosisHeightMinimum = 520;
    private const double PSDiagnosisInset = 18;

    private static readonly Brush PSDiagnosisReadyFill = PSDiagnosisBrushCreate(0xE8, 0xF1, 0xE7);
    private static readonly Brush PSDiagnosisReadyInk = PSDiagnosisBrushCreate(0x2E, 0x5B, 0x2B);
    private static readonly Brush PSDiagnosisMissingFill = PSDiagnosisBrushCreate(0xFB, 0xE3, 0xE3);
    private static readonly Brush PSDiagnosisMissingInk = PSDiagnosisBrushCreate(0x8C, 0x1D, 0x1D);
    private static readonly Brush PSDiagnosisReadyDot = PSDiagnosisBrushCreate(0x2E, 0xA0, 0x43);
    private static readonly Brush PSDiagnosisWarnDot = PSDiagnosisBrushCreate(0xE0, 0xA0, 0x11);
    private static readonly Brush PSDiagnosisMissingDot = PSDiagnosisBrushCreate(0xD0, 0x33, 0x33);
    private static readonly Brush PSDiagnosisNeutralFill = PSDiagnosisBrushCreate(0xEC, 0xEF, 0xF4);
    private static readonly Brush PSDiagnosisChipFill = PSDiagnosisBrushCreate(0xF1, 0xF5, 0xFA);
    private static readonly FontFamily PSDiagnosisChipFont = new("Consolas, Cascadia Mono, monospace");

    private static PSDiagnosis? psDiagnosisCurrent;

    private readonly string psDiagnosisTitle;
    private readonly PSGrabber psDiagnosisGrabber;

    internal static void PSDiagnosisShow(Window pOwner)
    {
        if (psDiagnosisCurrent is not null)
        {
            psDiagnosisCurrent.Activate();
            return;
        }

        var psDiagnosis = new PSDiagnosis(pOwner);
        psDiagnosisCurrent = psDiagnosis;
        psDiagnosis.Show();
    }

    private PSDiagnosis(Window pOwner)
    {
        psDiagnosisTitle = LLocalization.LLocalizationTextRead("Diagnosis.Window.Title");
        Title = psDiagnosisTitle;
        Owner = pOwner.Owner ?? pOwner;
        ShowInTaskbar = true;
        Width = PSDiagnosisWidthDefault;
        Height = PSDiagnosisHeightDefault;
        MinWidth = PSDiagnosisWidthMinimum;
        MinHeight = PSDiagnosisHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
        PScrollbar.PScrollbarApply(this);
        Content = PSDiagnosisBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSDiagnosisPlacementKey);
        psDiagnosisGrabber = new PSGrabber(this);
        psDiagnosisGrabber.PSGrabberAttach();
        Closed += PSDiagnosisCloseHandle;
        PSDiagnosisProbeStart();
    }

    private UIElement PSDiagnosisBuild() =>
        PSDialog.PSDialogBuild(this, psDiagnosisTitle, PSDiagnosisRootBuild());

    private DockPanel PSDiagnosisRootBuild()
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
        Button pClose = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("Diagnosis.Button.Close"));
        pClose.Click += (_, _) => Close();
        pFooter.Children.Add(pClose);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);

        UIElement pBanner = PSDiagnosisBannerBuild();
        DockPanel.SetDock(pBanner, Dock.Top);
        pRoot.Children.Add(pBanner);

        ScrollViewer pScroll = PSSheet.PSSheetScrollBuild(PSDiagnosisContentBuild());
        pScroll.Margin = new Thickness(PSDiagnosisInset, 0, PSDiagnosisInset, 8);
        pRoot.Children.Add(pScroll);
        return pRoot;
    }

    private static Brush PSDiagnosisBrushCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    private void PSDiagnosisCloseHandle(object? pSender, EventArgs pEvent)
    {
        psDiagnosisGeneration++;
        PSGrabber.PSGrabberPlacementSave(this, PSDiagnosisPlacementKey);
        psDiagnosisGrabber.PSGrabberDetach();
        Closed -= PSDiagnosisCloseHandle;
        if (ReferenceEquals(psDiagnosisCurrent, this))
        {
            psDiagnosisCurrent = null;
        }
    }
}
