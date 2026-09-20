using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Cadroue.UIVeneer.PHouse;

using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using FlyleafLib;

namespace Cadroue.UIVeneer;

public partial class PProgram : System.Windows.Application
{
    private readonly LProgram lProgram = new();
    private readonly DispatcherTimer pProgramDebounce = new() { Interval = TimeSpan.FromMilliseconds(700) };

    public PProgram()
    {
        pProgramDebounce.Tick += PProgramDebounceHandle;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += PProgramDispatcherHandle;
        AppDomain.CurrentDomain.UnhandledException += PProgramDomainHandle;
        TaskScheduler.UnobservedTaskException += PProgramTaskHandle;

        lProgram.LProgramStart(
            e.Args,
            PProgramDispatch,
            PProgramDefer,
            typeof(PProgram).Assembly.GetManifestResourceNames,
            PProgramResourceRead,
            [PWing.PSEncoder.PSEncoderPlacementKey, PSOptions.PSOptionsPlacementKey],
            PProgramDebounceDefer,
            PProgramFlyleafStart,
            PProgramVerboseApply);
        PNameplate.PNameplateAttach();
        base.OnStartup(e);
        LList.LListRelayAttach(
            PWing.PList.PListDeliveredAdd,
            PWing.PList.PListDeliveredCommit,
            PWing.PList.PListDeliveredRemove,
            LAction.LActionAccept,
            PWing.PList.PListBatchRemove,
            PWing.PList.PListSourceRelease,
            PWing.PList.PListSourceClaim);

        MainWindow = new PWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        lProgram.LProgramClose(e.ApplicationExitCode);
        base.OnExit(e);
    }

    private static string? PProgramResourceRead(string pResourceName) =>
        LLocalization.LLocalizationStreamRead(typeof(PProgram).Assembly.GetManifestResourceStream(pResourceName));

    private void PProgramDispatch(Action lAction) => Dispatcher.Invoke(lAction);

    private void PProgramDefer(Action lAction) => Dispatcher.BeginInvoke(lAction);

    private void PProgramDebounceDefer()
    {
        pProgramDebounce.Stop();
        pProgramDebounce.Start();
    }

    private void PProgramDebounceHandle(object? sender, EventArgs e)
    {
        pProgramDebounce.Stop();
        lProgram.LProgramDebounceTick();
    }

    private static void PProgramFlyleafStart()
    {
        var pEngineConfig = new EngineConfig
        {
            UIRefresh = false,
            UIRefreshInterval = 250
        };
        PProgramLogApply(pEngineConfig, LTrace.LTraceVerbose);
        pEngineConfig.FFmpegPath = LRenderer.LRendererFfmpegResolve(pEngineConfig.FFmpegPath);
        Engine.Start(pEngineConfig);
    }

    private static void PProgramVerboseApply(bool lVerbose) => PProgramLogApply(Engine.Config, lVerbose);

    private static void PProgramLogApply(EngineConfig pEngineConfig, bool lVerbose)
    {
        pEngineConfig.LogLevel = PLook.PLookFlyleafLog[lVerbose];
        pEngineConfig.LogOutput = LRenderer.LRendererLogResolve(lVerbose);
    }

    private static void PProgramDispatcherHandle(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LTraceLog.LTraceErrorRecord("Unhandled UI exception", e.Exception);
        e.Handled = true;
    }

    private static void PProgramDomainHandle(object sender, UnhandledExceptionEventArgs e) =>
        LTraceLog.LTraceErrorRecord("Unhandled application exception", e.ExceptionObject as Exception);

    private static void PProgramTaskHandle(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LTraceLog.LTraceErrorRecord("Unobserved task exception", e.Exception);
        e.SetObserved();
    }
}
