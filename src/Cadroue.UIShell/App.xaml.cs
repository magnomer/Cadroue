using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainWindow;
using FlyleafLib;

namespace Cadroue.UIShell;

public partial class App : Application
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererDefaultCreate();
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceDefaultCreate();

    private static string? lDepotRootApplied;

    public static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LPreferenceStateCurrent.LPreferenceFfmpegFolder)
            ? LRendererSettingsCurrent.LRendererFfmpegLibraryFolder ?? string.Empty
            : LPreferenceStateCurrent.LPreferenceFfmpegFolder;

    public static string LRendererProgramCurrent =>
        LRendererSettings.LRendererProgramRead(LRendererFolderCurrent);

    private static DispatcherTimer? lPreferenceSaveTimer;

    public static LRelay? LRelayStartupPayload { get; private set; }

    public static LRelay? LRelayStartupTake()
    {
        LRelay? lRelayPayload = LRelayStartupPayload;
        LRelayStartupPayload = null;
        return lRelayPayload;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, pEvent) =>
        {
            LAppLog.LError("Unhandled UI exception", pEvent.Exception);
            pEvent.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, pEvent) =>
            LAppLog.LError("Unhandled application exception", pEvent.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, pEvent) =>
        {
            LAppLog.LError("Unobserved task exception", pEvent.Exception);
            pEvent.SetObserved();
        };

        LRelayStartupRead(e.Args);
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

        LPreferenceStateCurrent = LPreferenceStateStore.LPreferenceStateLoad();
        LTrace.LTraceVerbose = LPreferenceStateCurrent.LPreferenceLogVerbose;
        Cadroue.Core.LDepot.LDepotRootSet(LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
        LFlyleafLocal.LFlyleafLocalResolverRegister();
        base.OnStartup(e);
        PFlow.PSectionPalette.PSectionPaletteReload();
        LPlacementCarry();

        Cadroue.ShellEngine.LRunner.LRunnerReport = LRunnerReportHandle;
        Cadroue.ShellEngine.LRunner.LRunnerFfmpegReport = LRunnerFfmpegHandle;
        Cadroue.ShellEngine.LRunner.LRunnerVerboseSource = () => LTrace.LTraceVerbose;
        Cadroue.Core.LSchedule.LScheduleRecoverReport = LAppLog.LInfo;
        LAppLog.LInfo($"Application started: version {LAppVersionRead()}, process {Environment.ProcessId}");
        LAppLog.LInfo(LFlyleafLocal.LFlyleafLocalActive
            ? "Local Flyleaf preview engine active"
            : "NuGet Flyleaf preview engine active");
        LDepotRootApply();
        LScheduleRecoverRun();
        LRendererFlyleafStart();
        LRelayStore.LRelayStaleClear();
        LRelayChannel.LRelayChannelStart();

        MainWindow = new PWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LAppLog.LInfo($"Application exiting with code {e.ApplicationExitCode}");
        LRelayChannel.LRelayChannelStop();
        LTraceWriter.LTraceWriterPersist();
        base.OnExit(e);
    }

    private static void LRelayStartupRead(string[] lStartupArguments)
    {
        for (int lIndex = 0; lIndex < lStartupArguments.Length - 1; lIndex++)
        {
            if (!string.Equals(lStartupArguments[lIndex], PToolbar.PTabRelayArgument, StringComparison.Ordinal))
            {
                continue;
            }

            string lRelayFilePath = lStartupArguments[lIndex + 1];
            LRelayStartupPayload = LRelayStore.LRelayFileLoad(lRelayFilePath);
            LRelayStore.LRelayFileClear(lRelayFilePath);
            if (LRelayStartupPayload is { } lRelayPayload)
            {
                LAppLog.LInfo($"Started to receive a relayed '{lRelayPayload.LayoutKey}' tab");
                LRelayChannel.LRelayAckSend(lRelayPayload.SenderProcessId, lRelayPayload.RelayId);
            }

            return;
        }
    }

    public static void LPreferenceStateSet(LPreferenceState lPreferenceState)
    {
        lPreferenceSaveTimer?.Stop();
        lPreferenceState.LPreferenceNormalize();
        foreach (string lPreferenceChange in lPreferenceState.LPreferenceDifferenceRead(LPreferenceStateCurrent))
        {
            LAppLog.LInfo($"Preference changed — {lPreferenceChange}");
        }

        LPreferenceStateCurrent = lPreferenceState;
        LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
        LDepotRootApply();
        LSidecarFolderApply();
    }

    public static void LSidecarFolderApply()
    {
        Cadroue.Media.LSidecarStore.LSidecarFolderSet(
            System.IO.Path.Combine(
                Cadroue.Core.LDepot.LDepotRootRead(),
                Cadroue.Media.LSidecarStore.LSidecarRecordFolderName),
            LPreferenceStateCurrent.LPreferenceRecordWorkspace);
    }

    private static void LDepotRootApply()
    {
        try
        {
            Cadroue.Core.LDepot.LDepotRootSet(LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
            string lDepotRoot = Cadroue.Core.LDepot.LDepotRootRead();
            if (string.Equals(lDepotRoot, lDepotRootApplied, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (lDepotRootApplied is string lDepotPrevious && !LDepotCarry(lDepotPrevious, lDepotRoot))
            {
                Cadroue.Core.LDepot.LDepotRootSet(lDepotPrevious);
                return;
            }

            Cadroue.Core.LDepotIndex.LDepotIndexEnsure();
            lDepotRootApplied = lDepotRoot;
            LSidecarFolderApply();
            LAppLog.LInfo($"Workspace at {lDepotRoot}");
        }
        catch (Exception lException)
        {
            lDepotRootApplied = null;
            LAppLog.LError("Workspace folder could not be prepared", lException);
        }
    }

    private static void LPlacementCarry()
    {
        try
        {
            string lPreferencePath = LPreferenceStateStore.LPreferencePathRead();
            if (!System.IO.File.Exists(lPreferencePath))
            {
                return;
            }

            using var lPreferenceDocument = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(lPreferencePath));
            System.Text.Json.JsonElement lPreferenceRoot = lPreferenceDocument.RootElement;
            LPlacementCarryOne(lPreferenceRoot, "Encoder", PPanels.PSEncoder.PSEncoderPlacementKey);
            LPlacementCarryOne(lPreferenceRoot, "Options", PSOptions.PSOptionsPlacementKey);
        }
        catch (Exception lException)
        {
            LAppLog.LError("Subwindow placement could not be carried from preferences", lException);
        }
    }

    private static void LPlacementCarryOne(System.Text.Json.JsonElement lPreferenceRoot, string lPrefix, string lPlacementKey)
    {
        if (Cadroue.Core.LPlacement.LPlacementExist(lPlacementKey)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Left", out System.Text.Json.JsonElement lLeft)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Top", out System.Text.Json.JsonElement lTop)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Width", out System.Text.Json.JsonElement lWidth)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Height", out System.Text.Json.JsonElement lHeight)
            || lLeft.ValueKind != System.Text.Json.JsonValueKind.Number
            || lTop.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            return;
        }

        Cadroue.Core.LPlacement.LPlacementSave(
            lPlacementKey, lLeft.GetDouble(), lTop.GetDouble(), lWidth.GetDouble(), lHeight.GetDouble());
        LAppLog.LInfo($"Subwindow placement carried from preferences: {lPlacementKey}");
    }

    private static void LScheduleRecoverRun()
    {
        try
        {
            int lScheduleRecovered = Cadroue.Core.LSchedule.LScheduleCurrent.LScheduleStaleClaim();
            if (lScheduleRecovered > 0)
            {
                LAppLog.LInfo($"Worklist recovery: {lScheduleRecovered} interrupted job(s) resolved at startup");
            }
        }
        catch (Exception lException)
        {
            LAppLog.LError("Worklist recovery failed at startup", lException);
        }
    }

    private static void LRunnerReportHandle(string lRunnerMessage, Exception? lRunnerException)
    {
        if (lRunnerException is null)
        {
            LAppLog.LInfo(lRunnerMessage);
            return;
        }

        LAppLog.LError(lRunnerMessage, lRunnerException);
    }

    private static void LRunnerFfmpegHandle(string lRunnerSummary, string? lRunnerDetail)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceFfmpeg, lRunnerSummary, lRunnerDetail);
    }

    private static string LAppVersionRead() =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

    private static bool LDepotCarry(string lDepotPrevious, string lDepotNext)
    {
        try
        {
            if (Cadroue.Core.LDepot.LDepotRunningCheck(lDepotPrevious))
            {
                LAppLog.LError($"Workspace kept at {lDepotPrevious}: a job is running, so nothing was moved");
                return false;
            }

            Cadroue.Core.LDepot.LDepotMove(lDepotPrevious, lDepotNext);
            LAppLog.LInfo($"Workspace moved from {lDepotPrevious}");
            return true;
        }
        catch (Exception lException)
        {
            LAppLog.LError($"Workspace kept at {lDepotPrevious}: the move failed", lException);
            return false;
        }
    }

    public static void LPreferenceVolumeSet(double lPreferenceVolume)
    {
        double lVolume = LPreferenceState.LPreferenceVolumeClamp(lPreferenceVolume);
        if (Math.Abs(lVolume - LPreferenceStateCurrent.LPreferenceVolume) < 0.0001)
        {
            return;
        }

        LPreferenceStateCurrent = LPreferenceStateCurrent.LPreferenceVolumeChange(lVolume);
        LPreferenceSchedule();
    }

    public static void LPreferenceMediaSet(string? lPreferenceMediaPath)
    {
        string lMediaPath = (lPreferenceMediaPath ?? string.Empty).Trim();
        if (string.Equals(lMediaPath, LPreferenceStateCurrent.LPreferenceMediaPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceMediaPath = lMediaPath;
        LPreferenceStateCurrent = lPreferenceNext;
        LPreferenceSchedule();
    }

    private static void LPreferenceSchedule()
    {
        lPreferenceSaveTimer ??= LPreferenceTimerCreate();
        lPreferenceSaveTimer.Stop();
        lPreferenceSaveTimer.Start();
    }

    private static DispatcherTimer LPreferenceTimerCreate()
    {
        var lPreferenceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        lPreferenceTimer.Tick += (_, _) =>
        {
            lPreferenceTimer.Stop();
            LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
        };
        return lPreferenceTimer;
    }

    private static void LRendererFlyleafStart()
    {
        try
        {
            var lRendererEngineConfig = new EngineConfig
            {
                UIRefresh = false,
                UIRefreshInterval = 250,
                LogLevel = LogLevel.Debug,
                LogOutput = System.IO.Path.Combine(Cadroue.Core.LDepot.LDepotRootRead(), "log", "flyleaf-debug.log")
            };

            if (LRendererSettings.LRendererFolderValidate(LPreferenceStateCurrent.LPreferenceFfmpegFolder))
            {
                lRendererEngineConfig.FFmpegPath = LPreferenceStateCurrent.LPreferenceFfmpegFolder;
            }
            else if (LRendererSettingsCurrent.LRendererFfmpegLibraryFolderCustomReady)
            {
                lRendererEngineConfig.FFmpegPath = LRendererSettingsCurrent.LRendererFfmpegLibraryFolder;
            }
            else
            {
                string? lRendererLocalPath = LRendererSettings.LRendererFolderFind();
                if (lRendererLocalPath is not null)
                {
                    lRendererEngineConfig.FFmpegPath = lRendererLocalPath;
                }
            }

            LAppLog.LInfo(LFlyleafLocal.LFlyleafLocalLoadedReportRead(typeof(Engine).Assembly));
            Engine.Start(lRendererEngineConfig);
        }
        catch (Exception lException)
        {
            LAppLog.LError("Renderer startup failed", lException);
        }
    }
}
