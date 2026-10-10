using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using FlyleafLib;

int code = 1;
Thread thread = new(() =>
{
    PProgram application = new();
    LProgram program = new();
    DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(700) };
    Action<EngineConfig, bool> logApply = (config, verbose) =>
    {
        config.LogLevel = PLook.PLookFlyleafLog[verbose];
        config.LogOutput = LRenderer.LRendererLogResolve(verbose);
    };

    debounce.Tick += (_, _) =>
    {
        debounce.Stop();
        program.LProgramDebounceTick();
    };
    application.DispatcherUnhandledException += (_, e) =>
    {
        LTraceLog.LTraceErrorRecord("Unhandled UI exception", e.Exception);
        e.Handled = true;
    };
    AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        LTraceLog.LTraceErrorRecord("Unhandled application exception", e.ExceptionObject as Exception);
    TaskScheduler.UnobservedTaskException += (_, e) =>
    {
        LTraceLog.LTraceErrorRecord("Unobserved task exception", e.Exception);
        e.SetObserved();
    };

    application.Startup += (_, e) =>
    {
        program.LProgramStart(
            e.Args,
            action => application.Dispatcher.Invoke(action),
            action => application.Dispatcher.BeginInvoke(action),
            typeof(PProgram).Assembly.GetManifestResourceNames,
            name => LLocalization.LLocalizationStreamRead(typeof(PProgram).Assembly.GetManifestResourceStream(name)),
            [PSEncoder.PSEncoderPlacementKey, PSOptions.PSOptionsPlacementKey],
            () =>
            {
                debounce.Stop();
                debounce.Start();
            },
            () =>
            {
                EngineConfig config = new() { UIRefresh = false, UIRefreshInterval = 250 };
                logApply(config, LTrace.LTraceVerbose);
                config.FFmpegPath = LRenderer.LRendererFfmpegResolve(config.FFmpegPath);
                Engine.Start(config);
            },
            verbose => logApply(Engine.Config, verbose));
        PNameplate.PNameplateAttach();
        application.MainWindow = new PWindow();
        application.MainWindow.Show();
    };
    application.Exit += (_, e) => program.LProgramClose(e.ApplicationExitCode);

    code = application.Run();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return code;
