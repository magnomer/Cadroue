using System.Windows;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPorch;

internal sealed class PRailRelay
{
    private static readonly IReadOnlyDictionary<QRailRelayOutcome, Action<Window>> pRailOutcomes =
        new Dictionary<QRailRelayOutcome, Action<Window>>
        {
            [QRailRelayOutcome.QRailRelaySkipped] = pWindow => { },
            [QRailRelayOutcome.QRailRelayBusy] = PTabBusyShow,
            [QRailRelayOutcome.QRailRelayRelayed] = pWindow => { },
            [QRailRelayOutcome.QRailRelayCopied] = pWindow => { },
            [QRailRelayOutcome.QRailRelayKept] = PTabKeptShow,
        };

    private readonly QRailRelay lRelay;
    private readonly FrameworkElement pRailElement;

    internal PRailRelay(QRailRelay lRelayOwner, FrameworkElement pRailOwner)
    {
        lRelay = lRelayOwner;
        pRailElement = pRailOwner;
    }

    internal async void PTabRelayRun(QStripTab? lStripTab, Point pDevicePoint)
    {
        Window pWindow = Window.GetWindow(pRailElement)!;
        Point pWindowPoint = pWindow.PointFromScreen(pDevicePoint);
        bool pInside = QRail.QRailInsideCheck(
            PLook.PLookMinimized[pWindow.WindowState],
            pWindowPoint.X,
            pWindowPoint.Y,
            pWindow.ActualWidth,
            pWindow.ActualHeight);
        Point pDipPoint = QSashPointer.QSashPointerResolve(pWindow, pDevicePoint);
        QRailRelayOutcome lOutcome = await lRelay.QRailRelayRun(
            lStripTab, pInside, pDipPoint.X, pDipPoint.Y, pDevicePoint.X, pDevicePoint.Y);
        pRailOutcomes[lOutcome](pWindow);
    }

    private static void PTabKeptShow(Window pWindow) =>
        PSWarning.PSWarningShow(
            pWindow,
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.KeptTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.KeptMessage"));

    private static void PTabBusyShow(Window pWindow) =>
        PSAnnouncement.PSAnnouncementShow(
            pWindow,
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.BusyTitle"),
            QLocalizationCatalog.QLocalizationTextRead("Tab.Relay.BusyMessage"));
}
