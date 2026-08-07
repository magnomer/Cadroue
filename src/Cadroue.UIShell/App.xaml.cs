using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainWindow;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

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

    private static void LStationSeamApply()
    {
        LStation.LStationSchedule = LScheduleCurrent;
        LStation.LStationPost = LStationDispatch;
        LStation.LStationProgramSource = () => LRenderer.LRendererProgramCurrent;
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        LSchedule.LScheduleCohortGate = Cadroue.MigrationInterface.LSeal.LSealClaimCheck;

        Cadroue.MigrationInterface.LMessenger.LMessengerScheduleSource = () => LScheduleCurrent;
        Cadroue.MigrationInterface.LCartographer.LCartographerTabsSource = () =>
            PControlBar.PStrip.PStripCurrent?.PStripRecords.Select(pTab =>
                new Cadroue.MigrationInterface.LCartographerTab(
                    pTab.PTabId,
                    pTab.PTabLayoutKey,
                    pTab.PTabTitle,
                    pTab.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate(),
                    pTab.PTabWorkspace.PWorkspaceLayoutRead().LSceneTabClone(),
                    pTab.PTabWorkspace.PWorkspaceSurface is PMainArea.PFunnelTab)).ToArray()
            ?? (IReadOnlyList<Cadroue.MigrationInterface.LCartographerTab>)Array.Empty<Cadroue.MigrationInterface.LCartographerTab>();
        Cadroue.MigrationInterface.LMessenger.LMessengerTitleSource = PControlBar.PStrip.PStripTitleRead;
        Cadroue.MigrationInterface.LCartographer.LCartographerTitleSource = PControlBar.PStrip.PStripTitleRead;
        Cadroue.MigrationInterface.LMessenger.LMessengerRouteSource =
            (lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan) =>
                PMainArea.LCourier.LCourierScheduleAdd(lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan);

        Cadroue.MigrationInterface.LSeal.LSealNodesSource = () =>
            PControlBar.PStrip.PStripCurrent?.PStripRecords
                .Where(pTab => pTab.PTabWorkspace.PWorkspaceSurface.PTabList is not null)
                .Select(pTab =>
                {
                    PMainArea.PTabSurface pSurface = pTab.PTabWorkspace.PWorkspaceSurface;
                    return new Cadroue.MigrationInterface.LSealNode(
                        pTab.PTabId,
                        pSurface is PMainArea.PMergeTab,
                        pSurface.PTabAction is { PActionAutoRelay: true },
                        pSurface.PTabList!.PListItemsRead()
                            .Where(pItem => pItem.PListItemDelivered && pItem.PListItemRelay != Guid.Empty)
                            .Select(pItem => pItem.PListItemRelay)
                            .Distinct()
                            .ToArray());
                })
                .ToArray();
        Cadroue.MigrationInterface.LSeal.LSealFireSeam = lSealNodeId =>
            PControlBar.PStrip.PStripCurrent?.PStripRecords
                .FirstOrDefault(pTab => pTab.PTabId == lSealNodeId)
                ?.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionAllRun();
        Cadroue.MigrationInterface.LSeal.LSealDeliveredSource = PMainArea.LCourier.LCourierDeliveredCheck;

        Cadroue.MigrationInterface.LPreview.LPreviewApplySeam = PPanels.PViewer.PViewerPlayerApply;
    }

    private static void LStationDispatch(Action lStationAction)
    {
        if (Current?.Dispatcher is { } lStationDispatcher)
        {
            lStationDispatcher.Invoke(lStationAction);
            return;
        }

        lStationAction();
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

        LPreference.LPreferenceDepotCallback = LPreferenceDepotHandle;
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
        Cadroue.Infrastructure.LPlacement.LPlacementImport(
            LPreferenceStateStore.LPreferencePathRead(),
            new (string, string)[]
            {
                ("Encoder", PPanels.PSEncoder.PSEncoderPlacementKey),
                ("Options", PSOptions.PSOptionsPlacementKey),
            });

        Cadroue.Media.LTool.LToolFolderSource = () => LRenderer.LRendererFolderCurrent;
        LStationSeamApply();
        _ = System.Threading.Tasks.Task.Run(Cadroue.Infrastructure.LInventory.LInventoryInstalledRead);
        PPanels.PSEncoder.PSCodecProbeStart();
        Cadroue.ShellEngine.LRunner.LRunnerReport = LRunnerReportHandle;
        Cadroue.ShellEngine.LRunner.LRunnerFfmpegReport = LRunnerFfmpegHandle;
        Cadroue.ShellEngine.LRunner.LRunnerVerboseSource = () => LTrace.LTraceVerbose;
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
            if (!string.Equals(lStartupArguments[lIndex], PTabNavigator.PTabRelayArgument, StringComparison.Ordinal))
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

    private static void LPreferenceDepotHandle()
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

            if (lDepotRootApplied is string lDepotPrevious
                && !Cadroue.Infrastructure.LDepot.LDepotFolderMove(lDepotPrevious, lDepotRoot))
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

}
