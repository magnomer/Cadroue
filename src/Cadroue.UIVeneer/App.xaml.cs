using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer;

public partial class PProgram : System.Windows.Application
{
    private PDebounce pProgramDebounce = null!;

    public PProgram()
    {
        InitializeComponent();
    }

    public static IReadOnlyList<string> PProgramPlacementKeys =>
        [PSEncoder.PSEncoderPlacementKey, PSOptions.PSOptionsPlacementKey];

    public void PProgramAttach(Action<string, Exception?> pFault, Action pTick)
    {
        pProgramDebounce = new PDebounce(TimeSpan.FromMilliseconds(700), pTick);
        PProgramFaultAttach(pFault);
    }

    private void PProgramFaultAttach(Action<string, Exception?> pFault)
    {
        DispatcherUnhandledException += (_, e) =>
        {
            pFault("Unhandled UI exception", e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            pFault("Unhandled application exception", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            pFault("Unobserved task exception", e.Exception);
            e.SetObserved();
        };
    }

    public void PProgramDispatch(Action pAction) => Dispatcher.Invoke(pAction);

    public void PProgramDefer(Action pAction) => Dispatcher.BeginInvoke(pAction);

    public void PProgramDebounceDefer() => pProgramDebounce.PDebounceDefer();

    public void PProgramWindowShow(QWindow lWindow)
    {
        PWindow pWindow = new(lWindow);
        QNameplateTip.QNameplateTipAttach();
        PProgramMainShow(pWindow);
    }

    private void PProgramMainShow(PWindow pWindow)
    {
        MainWindow = pWindow;
        MainWindow.Show();
    }
}
