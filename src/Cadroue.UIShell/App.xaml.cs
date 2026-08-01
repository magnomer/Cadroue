using System;
using System.Threading.Tasks;
using System.Windows;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainWindow;

using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

public partial class PProgram : System.Windows.Application
{
    public static LScheduleContract LScheduleCurrent { get; private set; } = new Cadroue.Infrastructure.LSchedule();

    private static string? lDepotRootApplied;

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
        LRenderer.LRendererSettingsLoad();

        LPreference.LPreferenceWorkspaceCallback = LPreferenceWorkspaceHandle;
        LPreference.LPreferenceLoad();
        Cadroue.Infrastructure.LFrameStore.LFrameLoad();
        LBinding.LBindingLoad();
        LScene.LSceneCurrentLoad();
        LLocalization.LLocalizationLoad(LPreference.LPreferenceStateCurrent.LPreferenceLanguage);
        LTrace.LTraceVerbose = LPreference.LPreferenceStateCurrent.LPreferenceLogVerbose;
        Cadroue.Infrastructure.LDepot.LDepotRootSet(LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
        LFlyleaf.LFlyleafResolverAttach();
        base.OnStartup(e);
        PFlow.PSectionPalette.PSectionPaletteLoad();
        LPlacementImport();

        Cadroue.Media.LTool.LToolFolderSource = () => LRenderer.LRendererFolderCurrent;
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
        LRenderer.LRendererFlyleafStart();
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
            }

            return;
        }
    }

    private static void LPreferenceWorkspaceHandle()
    {
        LDepotRootApply();
        LSidecarFolderApply();
    }

    public static void LSidecarFolderApply()
    {
        Cadroue.Media.LSidecarStore.LSidecarFolderSet(
            System.IO.Path.Combine(
                Cadroue.Infrastructure.LDepot.LDepotRootRead(),
                Cadroue.Media.LSidecarStore.LSidecarRecordFolder),
            LPreference.LPreferenceStateCurrent.LPreferenceRecordWorkspace);
    }

    private static void LDepotRootApply()
    {
        try
        {
            Cadroue.Infrastructure.LDepot.LDepotRootSet(LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
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
            int lScheduleRecovered = LScheduleCurrent.LScheduleStaleClaim();
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

}
