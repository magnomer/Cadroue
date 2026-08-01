using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainWindow;
using FlyleafLib;

namespace Cadroue.UIShell;

public partial class PProgram : Application
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererDefaultCreate();
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceDefaultCreate();

    private static string? lDepotRootApplied;

    public static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LPreferenceStateCurrent.LPreferenceFfmpegFolder)
            ? LRendererSettingsCurrent.LRendererLibraryFolder ?? string.Empty
            : LPreferenceStateCurrent.LPreferenceFfmpegFolder;

    public static string LRendererProgramCurrent =>
        LRendererSettings.LRendererProgramRead(LRendererFolderCurrent);

    private static DispatcherTimer? lPreferenceSaveTimer;

    public static LRelay? LRelayStartupPayload { get; private set; }

    public static LRelay? LRelayPayloadRead()
    {
        LRelay? lRelayPayload = LRelayStartupPayload;
        LRelayStartupPayload = null;
        return lRelayPayload;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, pEvent) =>
        {
            LTraceLog.LTraceErrorRecord("Unhandled UI exception", pEvent.Exception);
            pEvent.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, pEvent) =>
            LTraceLog.LTraceErrorRecord("Unhandled application exception", pEvent.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, pEvent) =>
        {
            LTraceLog.LTraceErrorRecord("Unobserved task exception", pEvent.Exception);
            pEvent.SetObserved();
        };

        LRelayStartupRead(e.Args);
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

        LPreferenceStateCurrent = LPreferenceStateStore.LPreferenceStateLoad();
        LLocalization.LLocalizationLoad(LPreferenceStateCurrent.LPreferenceLanguage);
        LTrace.LTraceVerbose = LPreferenceStateCurrent.LPreferenceLogVerbose;
        Cadroue.Infrastructure.LDepot.LDepotRootSet(LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
        LFlyleaf.LFlyleafResolverAttach();
        base.OnStartup(e);
        PFlow.PSectionPalette.PSectionPaletteLoad();
        LPlacementImport();

        Cadroue.Media.LTool.LToolFolderSource = () => LRendererFolderCurrent;
        _ = System.Threading.Tasks.Task.Run(Cadroue.Infrastructure.LInventory.LInventoryInstalledRead);
        PPanels.PSEncoder.PSCodecProbeStart();
        Cadroue.ShellEngine.LRunner.LRunnerReport = LRunnerReportHandle;
        Cadroue.ShellEngine.LRunner.LRunnerFfmpegReport = LRunnerFfmpegHandle;
        Cadroue.ShellEngine.LRunner.LRunnerVerboseSource = () => LTrace.LTraceVerbose;
        Cadroue.Infrastructure.LSchedule.LScheduleRecoverReport = LTraceLog.LTraceInfoRecord;
        LTraceLog.LTraceInfoRecord($"Application started: version {PProgramVersionRead()}, process {Environment.ProcessId}");
        LTraceLog.LTraceInfoRecord(LFlyleaf.LFlyleafActive
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
        LTraceLog.LTraceInfoRecord($"Application exiting with code {e.ApplicationExitCode}");
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
                LTraceLog.LTraceInfoRecord($"Started to receive a relayed '{lRelayPayload.LRelayLayoutKey}' tab");
                LRelayChannel.LRelayAckSend(lRelayPayload.LRelaySenderProcess, lRelayPayload.LRelayId);
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
            LTraceLog.LTraceInfoRecord($"Preference changed — {lPreferenceChange}");
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
                Cadroue.Infrastructure.LDepot.LDepotRootRead(),
                Cadroue.Media.LSidecarStore.LSidecarRecordFolder),
            LPreferenceStateCurrent.LPreferenceRecordWorkspace);
    }

    private static void LDepotRootApply()
    {
        try
        {
            Cadroue.Infrastructure.LDepot.LDepotRootSet(LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
            string lDepotRoot = Cadroue.Infrastructure.LDepot.LDepotRootRead();
            if (string.Equals(lDepotRoot, lDepotRootApplied, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (lDepotRootApplied is string lDepotPrevious && !LDepotFolderMove(lDepotPrevious, lDepotRoot))
            {
                Cadroue.Infrastructure.LDepot.LDepotRootSet(lDepotPrevious);
                return;
            }

            Cadroue.Infrastructure.LDepotIndex.LDepotIndexCreate();
            lDepotRootApplied = lDepotRoot;
            LSidecarFolderApply();
            LTraceLog.LTraceInfoRecord($"Workspace at {lDepotRoot}");
        }
        catch (Exception lException)
        {
            lDepotRootApplied = null;
            LTraceLog.LTraceErrorRecord("Workspace folder could not be prepared", lException);
        }
    }

    private static void LPlacementImport()
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
            LPlacementEntryImport(lPreferenceRoot, "Encoder", PPanels.PSEncoder.PSEncoderPlacementKey);
            LPlacementEntryImport(lPreferenceRoot, "Options", PSOptions.PSOptionsPlacementKey);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Subwindow placement could not be carried from preferences", lException);
        }
    }

    private static void LPlacementEntryImport(System.Text.Json.JsonElement lPreferenceRoot, string lPrefix, string lPlacementKey)
    {
        if (Cadroue.Infrastructure.LPlacement.LPlacementExist(lPlacementKey)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Left", out System.Text.Json.JsonElement lLeft)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Top", out System.Text.Json.JsonElement lTop)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Width", out System.Text.Json.JsonElement lWidth)
            || !lPreferenceRoot.TryGetProperty($"LPreference{lPrefix}Height", out System.Text.Json.JsonElement lHeight)
            || lLeft.ValueKind != System.Text.Json.JsonValueKind.Number
            || lTop.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            return;
        }

        Cadroue.Infrastructure.LPlacement.LPlacementSave(
            lPlacementKey, lLeft.GetDouble(), lTop.GetDouble(), lWidth.GetDouble(), lHeight.GetDouble());
        LTraceLog.LTraceInfoRecord($"Subwindow placement carried from preferences: {lPlacementKey}");
    }

    private static void LScheduleRecoverRun()
    {
        try
        {
            int lScheduleRecovered = Cadroue.Infrastructure.LSchedule.LScheduleCurrent.LScheduleStaleClaim();
            if (lScheduleRecovered > 0)
            {
                LTraceLog.LTraceInfoRecord($"Worklist recovery: {lScheduleRecovered} interrupted job(s) resolved at startup");
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Worklist recovery failed at startup", lException);
        }
    }

    private static void LRunnerReportHandle(string lRunnerMessage, Exception? lRunnerException)
    {
        if (lRunnerException is null)
        {
            LTraceLog.LTraceInfoRecord(lRunnerMessage);
            return;
        }

        LTraceLog.LTraceErrorRecord(lRunnerMessage, lRunnerException);
    }

    private static void LRunnerFfmpegHandle(string lRunnerSummary, string? lRunnerDetail)
    {
        LTrace.LTraceRecord(LTraceKind.LTraceFfmpeg, lRunnerSummary, lRunnerDetail);
    }

    private static string PProgramVersionRead() =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

    private static bool LDepotFolderMove(string lDepotPrevious, string lDepotNext)
    {
        try
        {
            if (Cadroue.Infrastructure.LDepot.LDepotRunningCheck(lDepotPrevious))
            {
                LTraceLog.LTraceErrorRecord($"Workspace kept at {lDepotPrevious}: a job is running, so nothing was moved");
                return false;
            }

            Cadroue.Infrastructure.LDepot.LDepotMove(lDepotPrevious, lDepotNext);
            LTraceLog.LTraceInfoRecord($"Workspace moved from {lDepotPrevious}");
            return true;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Workspace kept at {lDepotPrevious}: the move failed", lException);
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
        LPreferenceDefer();
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
        LPreferenceDefer();
    }

    public static void LPreferenceAutoSet(bool lPreferenceAutoResume)
    {
        if (lPreferenceAutoResume == LPreferenceStateCurrent.LPreferenceAutoActive)
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceAutoActive = lPreferenceAutoResume;
        LPreferenceStateCurrent = lPreferenceNext;
        LPreferenceDefer();
    }

    public static void LPreferenceSceneSet(string lPreferenceSceneName)
    {
        if (string.Equals(lPreferenceSceneName, LPreferenceStateCurrent.LPreferenceSceneName, StringComparison.Ordinal))
        {
            return;
        }

        LPreferenceState lPreferenceNext = LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceNext.LPreferenceSceneName = lPreferenceSceneName;
        LPreferenceStateCurrent = lPreferenceNext;
        LPreferenceDefer();
    }

    private static void LPreferenceDefer()
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
                UIRefreshInterval = 250
            };
            LRendererLogApply(lRendererEngineConfig, LTrace.LTraceVerbose);

            if (LRendererSettings.LRendererFolderValidate(LPreferenceStateCurrent.LPreferenceFfmpegFolder))
            {
                lRendererEngineConfig.FFmpegPath = LPreferenceStateCurrent.LPreferenceFfmpegFolder;
            }
            else if (LRendererSettingsCurrent.LRendererLibraryReady)
            {
                lRendererEngineConfig.FFmpegPath = LRendererSettingsCurrent.LRendererLibraryFolder;
            }
            else
            {
                string? lRendererLocalPath = LRendererSettings.LRendererFolderFind();
                if (lRendererLocalPath is not null)
                {
                    lRendererEngineConfig.FFmpegPath = lRendererLocalPath;
                }
            }

            LTraceLog.LTraceInfoRecord(LFlyleaf.LFlyleafReportRead(typeof(Engine).Assembly));
            Engine.Start(lRendererEngineConfig);
            LTrace.LTraceVerboseCallback = LRendererVerboseApply;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer startup failed", lException);
        }
    }

    private static void LRendererLogApply(EngineConfig lRendererConfig, bool lRendererVerbose)
    {
        if (!lRendererVerbose)
        {
            lRendererConfig.LogLevel = LogLevel.Quiet;
            lRendererConfig.LogOutput = string.Empty;
            return;
        }

        string lRendererLogFolder = LFlyleaf.LFlyleafRootRead();
        System.IO.Directory.CreateDirectory(lRendererLogFolder);
        lRendererConfig.LogLevel = LogLevel.Debug;
        lRendererConfig.LogOutput = System.IO.Path.Combine(lRendererLogFolder, "flyleaf-debug.log");
    }

    private static void LRendererVerboseApply(bool lRendererVerbose)
    {
        try
        {
            LRendererLogApply(Engine.Config, lRendererVerbose);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer log switch failed", lException);
        }
    }
}
