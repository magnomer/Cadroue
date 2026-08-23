using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainWindow;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell;

public partial class PProgram : System.Windows.Application
{
    public static LScheduleContract LScheduleCurrent { get; private set; } = new Cadroue.Infrastructure.LSchedule();

    private static string? lDepotRootApplied;

    private static void LPreferenceDebounceApply()
    {
        var lPreferenceTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        lPreferenceTimer.Tick += (_, _) =>
        {
            lPreferenceTimer.Stop();
            LPreference.LPreferenceSaveCommit();
        };
        LPreference.LPreferenceDebounceSeam = () =>
        {
            lPreferenceTimer.Stop();
            lPreferenceTimer.Start();
        };
    }

    private static void LPresetSelectionAttach()
    {
        Cadroue.Application.LPresetSelection.LPresetLoadSeam = lName =>
            LPreset.LPresetRead(lName)?.LPresetRecordCreate();
        Cadroue.Application.LPresetSelection.LPresetSaveSeam = (lName, lRecord) =>
            LPreset.LPresetSave(lName, LPreset.LPresetStateCreate(lRecord));
        Cadroue.Application.LPresetSelection.LPresetRenameSeam = (lOldName, lNewName, lRecord) =>
            LPreset.LPresetNameSet(lOldName, lNewName, LPreset.LPresetStateCreate(lRecord));
        Cadroue.Application.LPresetSelection.LPresetOutputSeam = lRecord =>
            LPreset.LPresetStateCreate(lRecord).LPresetOutputCreate();
    }

    private static void LStationSeamApply()
    {
        LStation.LStationSchedule = LScheduleCurrent;
        LStation.LStationPost = LStationDispatch;
        LStation.LStationProgramSource = () => Cadroue.Infrastructure.LRenderer.LRendererProgramCurrent;
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;

        Cadroue.ShellEngine.LMessenger.LMessengerScheduleSource = () => LScheduleCurrent;
        Cadroue.ShellEngine.LCartographer.LCartographerTabsSource = () =>
            PControlBar.PStrip.PStripCurrent?.PStripRecords.Select(pTab =>
                new Cadroue.ShellEngine.LCartographerTab(
                    pTab.PTabId,
                    pTab.PTabLayoutKey,
                    pTab.PTabTitle,
                    pTab.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate(),
                    pTab.PTabWorkspace.PWorkspaceLayoutRead().LSceneTabClone(),
                    pTab.PTabWorkspace.PWorkspaceSurface is PMainArea.PFunnelTab)).ToArray()
            ?? (IReadOnlyList<Cadroue.ShellEngine.LCartographerTab>)Array.Empty<Cadroue.ShellEngine.LCartographerTab>();
        Cadroue.ShellEngine.LMessenger.LMessengerTitleSource = PControlBar.PStrip.PStripTitleRead;
        Cadroue.ShellEngine.LCartographer.LCartographerTitleSource = PControlBar.PStrip.PStripTitleRead;
        Cadroue.ShellEngine.LCartographer.LCartographerScheduleContract = LScheduleCurrent;
        Cadroue.ShellEngine.LCartographer.LCartographerLockSeam = PPanels.PList.PListSourceClaim;
        Cadroue.ShellEngine.LCartographer.LCartographerDeliverySeam = new Cadroue.ShellEngine.LCartographerDelivery(
            PPanels.PList.PListDeliveredAdd,
            PPanels.PList.PListDeliveredPlace,
            PPanels.PList.PListDeliveredCommit,
            PPanels.PList.PListDeliveredRemove,
            PMainArea.PAction.PActionAccept,
            PPanels.PList.PListBatchRemove,
            PPanels.PList.PListSourceRelease);
        Cadroue.ShellEngine.LMessenger.LMessengerRouteSource =
            (lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan) =>
                Cadroue.ShellEngine.LCartographer.LCartographerAccept(
                    lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan);
        Cadroue.ShellEngine.LMessenger.LMessengerDeliverSource = (pFunnelTarget, pFunnelPath, pFunnelCohort) =>
        {
            if (!PPanels.PList.PListDeliveredAdd(pFunnelTarget, pFunnelPath, pFunnelCohort))
            {
                return false;
            }

            PMainArea.PAction.PActionAccept(pFunnelTarget, pFunnelPath, pFunnelCohort);
            return true;
        };
        Cadroue.ShellEngine.LMessenger.LMessengerDrainSource = pFunnelDrainPaths =>
        {
            PControlBar.PStrip.PStripCurrent?.PStripSelected?.PTabWorkspace.PWorkspaceSurface
                .PTabList?.PListDocketRead()?.LDocketPathsRemove(pFunnelDrainPaths);
        };

        Cadroue.ShellEngine.LSeal.LSealNodesSource = () =>
            PControlBar.PStrip.PStripCurrent?.PStripRecords
                .Where(pTab => pTab.PTabWorkspace.PWorkspaceSurface.PTabList is not null)
                .Select(pTab =>
                {
                    PMainArea.PTabSurface pSurface = pTab.PTabWorkspace.PWorkspaceSurface;
                    return new Cadroue.ShellEngine.LSealNode(
                        pTab.PTabId,
                        pSurface is PMainArea.PMergeTab,
                        pSurface.PTabAction is { PActionAutoRelay: true },
                        pSurface.PTabList!.PListItemsRead()
                            .Where(pItem => pItem.LDocketEntryDelivered && pItem.LDocketEntryBatch != Guid.Empty)
                            .Select(pItem => pItem.LDocketEntryBatch)
                            .Distinct()
                            .ToArray());
                })
                .ToArray();
        Cadroue.ShellEngine.LSeal.LSealFireSeam = lSealNodeId =>
            PControlBar.PStrip.PStripCurrent?.PStripRecords
                .FirstOrDefault(pTab => pTab.PTabId == lSealNodeId)
                ?.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionAllRun();

        Cadroue.Application.LPreview.LPreviewApplySeam = PPanels.PViewer.PViewerPlayerApply;
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

        LRelayChannel.LRelayStartupRead(e.Args);
        Cadroue.Infrastructure.LRenderer.LRendererSettingsLoad();

        LPreference.LPreferenceDepotCallback = LPreferenceDepotHandle;
        LPreference.LPreferenceLanguageSeam = LLocalization.LLocalizationLanguageNormalize;
        LPreference.LPreferenceLoadSeam = LPreferenceStateStore.LPreferenceStateLoad;
        LPreference.LPreferenceSaveSeam = LPreferenceStateStore.LPreferenceStateSave;
        LPreference.LPreferenceTraceSeam = LTraceLog.LTraceInfoRecord;
        LPreferenceDebounceApply();
        LPreference.LPreferenceLoad();
        PNameplate.PNameplateAttach();
        Cadroue.Infrastructure.LFrameStore.LFrameLoad();
        Cadroue.Infrastructure.LBinding.LBindingLoad();
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

        Cadroue.Media.LTool.LToolFolderSource = () => Cadroue.Infrastructure.LRenderer.LRendererFolderCurrent;
        LLibrarianSeamApply();
        Cadroue.Application.LSegment.LSegmentLoadSeam = LSidecarSectionsRead;
        Cadroue.Application.LSegment.LSegmentSaveSeam = (lSidecarSourcePath, lSidecarSections) =>
            Cadroue.Infrastructure.LSidecarStore.LSidecarSectionsSave(lSidecarSourcePath, lSidecarSections);
        LPresetSelectionAttach();
        LPreset.LPresetLoadSeam = LPresetStore.LPresetLoad;
        LPreset.LPresetSaveSeam = LPresetStore.LPresetSave;
        LPreset.LPresetPrepare();
        LStationSeamApply();
        _ = System.Threading.Tasks.Task.Run(Cadroue.Infrastructure.LInventory.LInventoryPrepare);
        PPanels.PSEncoder.PSCodecProbeStart();
        Cadroue.ShellEngine.LRunner.LRunnerReport = LRunnerReportHandle;
        Cadroue.ShellEngine.LRunner.LRunnerFfmpegReport = LRunnerFfmpegHandle;
        Cadroue.ShellEngine.LRunner.LRunnerVerboseSource = () => LTrace.LTraceVerbose;
        LTraceLog.LTraceLoadingRecord($"Application started: version {PProgramVersionRead()}, process {Environment.ProcessId}");
        LTraceLog.LTraceLoadingRecord(LFlyleaf.LFlyleafActive
            ? "Local Flyleaf preview engine active"
            : "NuGet Flyleaf preview engine active");
        LDepotRootApply();
        LRetentionSweepStart();
        LScheduleRecoverRun();
        Cadroue.Infrastructure.LRenderer.LRendererFlyleafSeam = LRendererFlyleafStart;
        Cadroue.Infrastructure.LRenderer.LRendererEngineStart();
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

    private static void LPreferenceDepotHandle()
    {
        LDepotRootApply();
        LSidecarFolderApply();
    }

    public static void LSidecarFolderApply()
    {
        Cadroue.Infrastructure.LSidecarStore.LSidecarFolderSet(
            System.IO.Path.Combine(
                Cadroue.Infrastructure.LDepot.LDepotRootRead(),
                Cadroue.Infrastructure.LSidecarStore.LSidecarRecordFolder),
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
            LTraceLog.LTraceLoadingRecord($"Workspace at {lDepotRoot}");
        }
        catch (Exception lException)
        {
            lDepotRootApplied = null;
            LTraceLog.LTraceErrorRecord("Workspace folder could not be prepared", lException);
        }
    }

    private static void LRetentionSweepStart()
    {
        if (!LPreference.LPreferenceStateCurrent.LPreferenceCleanupActive)
        {
            return;
        }

        int lRetentionDays = LPreference.LPreferenceStateCurrent.LPreferenceCleanupDays;
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            int lRetentionRemoved = Cadroue.Infrastructure.LRetentionSweep.LRetentionRun(lRetentionDays);
            LTraceLog.LTraceInfoRecord($"Retention sweep removed {lRetentionRemoved} old records");
        });
    }

    private static void LLibrarianSeamApply()
    {
        Cadroue.Application.LLibrarian.LLibrarianCoreReader = Cadroue.Infrastructure.LSidecarStore.LSidecarCoreRead;
        Cadroue.Application.LLibrarian.LLibrarianKeyframesSeam = Cadroue.Infrastructure.LSidecarStore.LSidecarKeyframesRead;
        Cadroue.Application.LLibrarian.LLibrarianWaveformReader = Cadroue.Infrastructure.LSidecarStore.LSidecarWaveformRead;
        Cadroue.Application.LLibrarian.LLibrarianEditReader = Cadroue.Infrastructure.LSidecarStore.LSidecarEditRead;
        Cadroue.Application.LLibrarian.LLibrarianAudioReader = Cadroue.Infrastructure.LSidecarStore.LSidecarAudioRead;
        Cadroue.Application.LLibrarian.LLibrarianSplitReader = Cadroue.Infrastructure.LSidecarStore.LSidecarSplitRead;
        Cadroue.Application.LLibrarian.LLibrarianLoudnessReader = Cadroue.Infrastructure.LSidecarStore.LSidecarLoudnessRead;
        Cadroue.Application.LLibrarian.LLibrarianDurationReader = Cadroue.Infrastructure.LSidecarStore.LSidecarDurationRead;
        Cadroue.Application.LLibrarian.LLibrarianDurationResolver = Cadroue.Infrastructure.LSidecarStore.LSidecarDurationResolve;
        Cadroue.Application.LLibrarian.LLibrarianEditWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarEditSave;
        Cadroue.Application.LLibrarian.LLibrarianAudioWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarAudioSave;
        Cadroue.Application.LLibrarian.LLibrarianSplitWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarSplitSave;
        Cadroue.Application.LLibrarian.LLibrarianLoudnessWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarLoudnessSave;
        Cadroue.Application.LLibrarian.LLibrarianWaveformWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarWaveformSave;
        Cadroue.Application.LLibrarian.LLibrarianFileChecker = Cadroue.Infrastructure.LSidecarStore.LSidecarFileCheck;
        Cadroue.Application.LLibrarian.LLibrarianSourceResolver = LSidecarSourceResolve;
        Cadroue.Application.LLibrarian.LLibrarianSourceMatcher = LSidecarStoreMatch;
    }

    private static Cadroue.Core.LSidecarSourceResult? LSidecarSourceResolve(string lSidecarPath) =>
        Cadroue.Infrastructure.LSidecarStore.LSidecarRead(lSidecarPath) is { } lSidecar
            ? Cadroue.Media.LSidecarSource.LSidecarSourceResolve(lSidecarPath, lSidecar)
            : null;

    private static bool LSidecarStoreMatch(string lSidecarMediaPath, string lSidecarPath) =>
        Cadroue.Infrastructure.LSidecarStore.LSidecarRead(lSidecarPath) is { } lSidecar
        && Cadroue.Media.LSidecarSource.LSidecarSourceMatch(lSidecarMediaPath, lSidecar.LSidecarSource);

    private static IReadOnlyList<Cadroue.Core.LSidecarSectionRecord> LSidecarSectionsRead(string lSidecarSourcePath)
    {
        try
        {
            if (Cadroue.Application.LLibrarian.LLibrarianLoad(lSidecarSourcePath) is { } lSidecarCore)
            {
                return lSidecarCore.LSidecarSections;
            }
        }
        catch (Exception lSidecarException)
        {
            LTraceLog.LTraceErrorRecord("Sidecar sections could not be restored", lSidecarException);
        }

        return Array.Empty<Cadroue.Core.LSidecarSectionRecord>();
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
