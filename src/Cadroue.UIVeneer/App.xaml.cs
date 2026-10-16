using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer;

public partial class PProgram : System.Windows.Application
{
    private readonly DispatcherTimer pProgramDebounce = new() { Interval = TimeSpan.FromMilliseconds(700) };

    public PProgram()
    {
        InitializeComponent();
    }

    public static IReadOnlyList<string> PProgramPlacementKeys =>
        [PSEncoder.PSEncoderPlacementKey, PSOptions.PSOptionsPlacementKey];

    public void PProgramAttach(Action<string, Exception?> pFault, Action pTick)
    {
        pProgramDebounce.Tick += (_, _) =>
        {
            pProgramDebounce.Stop();
            pTick();
        };
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

    public static string[] PProgramNamesRead() => typeof(PProgram).Assembly.GetManifestResourceNames();

    public static string? PProgramResourceRead(string pName) =>
        LLocalization.LLocalizationStreamRead(typeof(PProgram).Assembly.GetManifestResourceStream(pName));

    public void PProgramDebounceDefer()
    {
        pProgramDebounce.Stop();
        pProgramDebounce.Start();
    }

    public void PProgramWindowShow(QWindow lWindow)
    {
        PWindow pWindow = new(lWindow);
        QNameplateTip.QNameplateTipAttach();
        MainWindow = pWindow;
        MainWindow.Show();
    }
}
