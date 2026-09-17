using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Cadroue.UIVeneer.PToolbar;
using Cadroue.UIVeneer.PHouse;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer;

public partial class PProgram : System.Windows.Application
{
    public static LScheduleContract LScheduleCurrent { get; private set; } = new Cadroue.Infrastructure.LSchedule();

    private static string? lDepotRootApplied;

    private static void LPreferenceDebounceApply()
    {
        var lPreferenceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
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
            PToolbar.PStrip.PStripCurrent?.PStripRecords.Select(pTab =>
                new Cadroue.ShellEngine.LCartographerTab(
                    pTab.PTabId,
                    pTab.PTabLayoutKey,
                    pTab.PTabTitle,
                    pTab.PTabWorkspace.PWorkspaceExportState.LPresetRecordCreate(),
                    pTab.PTabWorkspace.PWorkspaceLayoutRead().LSceneTabClone(),
                    pTab.PTabWorkspace.PWorkspaceSurface is PDeck.PFunnelTab)).ToArray()
            ?? (IReadOnlyList<Cadroue.ShellEngine.LCartographerTab>)Array.Empty<Cadroue.ShellEngine.LCartographerTab>();
        Cadroue.ShellEngine.LMessenger.LMessengerTitleSource = PToolbar.PStrip.PStripTitleRead;
        Cadroue.ShellEngine.LCartographer.LCartographerTitleSource = PToolbar.PStrip.PStripTitleRead;
        Cadroue.ShellEngine.LCartographer.LCartographerScheduleContract = LScheduleCurrent;
        Cadroue.ShellEngine.LCartographer.LCartographerLockSeam = PPanel.PList.PListSourceClaim;
        Cadroue.ShellEngine.LCartographer.LCartographerDeliverySeam = new Cadroue.ShellEngine.LCartographerDelivery(
            PPanel.PList.PListDeliveredAdd,
            PPanel.PList.PListDeliveredCommit,
            PPanel.PList.PListDeliveredRemove,
            PDeck.PAction.PActionAccept,
            PPanel.PList.PListBatchRemove,
            PPanel.PList.PListSourceRelease);
        Cadroue.Core.LClassifier.LClassifierFaultSource =
            lClassifierFault => Cadroue.Infrastructure.LTraceLog.LTraceWarningRecord(lClassifierFault);
        Cadroue.ShellEngine.LMessenger.LMessengerRouteSource =
            (lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan) =>
                Cadroue.ShellEngine.LCartographer.LCartographerAccept(
                    lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan);
        Cadroue.ShellEngine.LMessenger.LMessengerDeliverSource = (pFunnelTarget, pFunnelPath, pFunnelCohort) =>
        {
            if (!PPanel.PList.PListDeliveredAdd(pFunnelTarget, pFunnelPath, pFunnelCohort))
            {
                return false;
            }

            PDeck.PAction.PActionAccept(pFunnelTarget, pFunnelPath, pFunnelCohort);
            return true;
        };
        Cadroue.ShellEngine.LMessenger.LMessengerDrainSource = (pFunnelSourceTab, pFunnelDrainPaths) =>
            PToolbar.PStrip.PStripTabFind(pFunnelSourceTab)?.PTabWorkspace.PWorkspaceSurface
                .PTabList?.PListDocketRead().LDocketPathsRemove(pFunnelDrainPaths);

        Cadroue.ShellEngine.LSeal.LSealNodesSource = () =>
            PToolbar.PStrip.PStripCurrent?.PStripRecords
                .Where(pTab => pTab.PTabWorkspace.PWorkspaceSurface.PTabList is not null)
                .Select(pTab =>
                {
                    PDeck.PTabSurface pSurface = pTab.PTabWorkspace.PWorkspaceSurface;
                    return new Cadroue.ShellEngine.LSealNode(
                        pTab.PTabId,
                        pSurface is PDeck.PMergeTab,
                        pSurface.PTabAction is { PActionAutoRelay: true },
                        pSurface.PTabList!.PListItemsRead()
                            .Where(pItem => pItem.LDocketEntryDelivered && pItem.LDocketEntryBatch != Guid.Empty)
                            .Select(pItem => pItem.LDocketEntryBatch)
                            .Distinct()
                            .ToArray());
                })
                .ToArray();
        Cadroue.ShellEngine.LSeal.LSealFireSeam = (lSealNodeId, lSealCohort) =>
            PToolbar.PStrip.PStripTabFind(lSealNodeId)?.PTabWorkspace.PWorkspaceSurface.PTabAction
                ?.PActionCohortRun(lSealCohort) == true;

        Cadroue.Application.LPreview.LPreviewApplySeam = PPanel.PViewer.PViewerPlayerApply;

        Cadroue.ShellEngine.LAutopsy.LAutopsyProse = LLocalizationAutopsy.LLocalizationAutopsyRead;
    }

    private static string? PProgramResourceRead(string pResourceName)
    {
        if (typeof(PProgram).Assembly.GetManifestResourceStream(pResourceName) is not { } pResourceStream)
        {
            return null;
        }

        using var pResourceReader = new System.IO.StreamReader(pResourceStream);
        return pResourceReader.ReadToEnd();
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

        LLocalization.LLocalizationNamesSeam = typeof(PProgram).Assembly.GetManifestResourceNames;
        LLocalization.LLocalizationTextSeam = PProgramResourceRead;
        LLocalization.LLocalizationTraceSeam = LTraceLog.LTraceErrorRecord;
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
        string lLocalizationLanguage = LLocalization.LLocalizationLanguageRead();
        if (!string.Equals(
                lLocalizationLanguage,
                LPreference.LPreferenceStateCurrent.LPreferenceLanguage,
                StringComparison.Ordinal))
        {
            LPreferenceState lLocalizationPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
            lLocalizationPreference.LPreferenceLanguage = lLocalizationLanguage;
            LPreference.LPreferenceStateSet(lLocalizationPreference);
        }

        LPreference.LPreferenceDepotCallback = LPreferenceDepotHandle;
        Cadroue.Infrastructure.LDepot.LDepotRootSet(LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
        LTrace.LTraceVerbose = LPreference.LPreferenceStateCurrent.LPreferenceLogVerbose;
        LFlyleaf.LFlyleafResolverAttach();
        base.OnStartup(e);
        PFlow.PSectionPalette.PSectionPaletteLoad();
        Cadroue.Infrastructure.LPlacement.LPlacementImport(
            LPreferenceStateStore.LPreferencePathRead(),
            new (string, string)[]
            {
                ("Encoder", PPanel.PSEncoder.PSEncoderPlacementKey),
                ("Options", PSOptions.PSOptionsPlacementKey),
            });

        Cadroue.Media.LTool.LToolFolderSource = () => Cadroue.Infrastructure.LRenderer.LRendererFolderCurrent;
        LLibrarianSeamApply();
        Cadroue.Application.LSegment.LSegmentLoadSeam = LSidecarSectionsRead;
        Cadroue.Application.LSegment.LSegmentSaveSeam = Cadroue.Infrastructure.LSidecarStore.LSidecarSectionsSave;
        LPresetSelectionAttach();
        LPreset.LPresetNativeSeam = LPresetStore.LPresetNativeLoad;
        LPreset.LPresetLoadSeam = LPresetStore.LPresetLoad;
        LPreset.LPresetSaveSeam = LPresetStore.LPresetSave;
        LPreset.LPresetTraceSeam = lPresetMessage => LTraceLog.LTraceWarningRecord(lPresetMessage);
        LPreset.LPresetPrepare();
        LStationSeamApply();
        Cadroue.Infrastructure.LInventory.LInventoryPrepareStart();
        Cadroue.Infrastructure.LTrialSet.LTrialSetStart();
        Cadroue.ShellEngine.LRunner.LRunnerReport = LRunnerReportHandle;
        Cadroue.ShellEngine.LRunner.LRunnerFfmpegReport = LRunnerFfmpegHandle;
        Cadroue.ShellEngine.LRunner.LRunnerVerboseSource = () => LTrace.LTraceVerbose;
        LTraceLog.LTraceLoadingRecord(
            $"Application started: version {PProgramVersionRead()}, process {Environment.ProcessId}");
        LTraceLog.LTraceLoadingRecord(LFlyleaf.LFlyleafActive
            ? "Local Flyleaf preview engine active"
            : "NuGet Flyleaf preview engine active");
        _ = LDepotRootApply();
        Cadroue.Infrastructure.LRetentionSweep.LRetentionSweepStart(
            LPreference.LPreferenceStateCurrent.LPreferenceCleanupActive,
            LPreference.LPreferenceStateCurrent.LPreferenceCleanupDays);
        LScheduleRecoverRun();
        Cadroue.Infrastructure.LRenderer.LRendererFlyleafSeam = LRendererFlyleafStart;
        Cadroue.Infrastructure.LRenderer.LRendererDispatchSeam = lAction => Dispatcher.BeginInvoke(lAction);
        Cadroue.Infrastructure.LRenderer.LRendererEngineStart();
        LRelayStore.LRelayStaleClear();
        LRelayChannel.LRelayChannelStart();

        MainWindow = new PWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LTraceLog.LTraceInfoRecord($"Application exiting with code {e.ApplicationExitCode}");
        LPreference.LPreferenceSaveCommit();
        LRelayChannel.LRelayChannelStop();
        LTraceWriter.LTraceWriterPersist();
        base.OnExit(e);
    }

    private static bool LPreferenceDepotHandle()
    {
        bool lDepotApplied = LDepotRootApply();
        LSidecarFolderApply();
        return lDepotApplied;
    }

    public static void LSidecarFolderApply()
    {
        Cadroue.Infrastructure.LSidecarStore.LSidecarFolderSet(
            System.IO.Path.Combine(
                Cadroue.Infrastructure.LDepot.LDepotRootRead(),
                Cadroue.Infrastructure.LSidecarStore.LSidecarRecordFolder),
            LPreference.LPreferenceStateCurrent.LPreferenceRecordWorkspace);
    }

    private static bool LDepotRootApply()
    {
        try
        {
            string lDepotRoot = Cadroue.Infrastructure.LDepot.LDepotRootResolve(
                LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder);
            if (string.Equals(lDepotRoot, lDepotRootApplied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (lDepotRootApplied is string lDepotPrevious
                && !Cadroue.Infrastructure.LDepot.LDepotFolderMove(lDepotPrevious, lDepotRoot))
            {
                return false;
            }

            if (lDepotRootApplied is null)
            {
                Cadroue.Infrastructure.LDepot.LDepotRootSet(lDepotRoot);
            }

            Cadroue.Infrastructure.LDepotIndex.LDepotIndexCreate();
            lDepotRootApplied = lDepotRoot;
            LSidecarFolderApply();
            LTraceLog.LTraceLoadingRecord($"Workspace at {lDepotRoot}");
            return true;
        }
        catch (Exception lException)
        {
            lDepotRootApplied = null;
            LTraceLog.LTraceErrorRecord("Workspace folder could not be prepared", lException);
            return false;
        }
    }

    private static void LLibrarianSeamApply()
    {
        Cadroue.Application.LLibrarian.LLibrarianCoreReader = Cadroue.Infrastructure.LSidecarStore.LSidecarCoreRead;
        Cadroue.Application.LLibrarian.LLibrarianKeyframesSeam =
            Cadroue.Infrastructure.LSidecarStore.LSidecarKeyframesRead;
        Cadroue.Application.LLibrarian.LLibrarianWaveformReader =
            Cadroue.Infrastructure.LSidecarStore.LSidecarWaveformRead;
        Cadroue.Application.LLibrarian.LLibrarianEditReader = Cadroue.Infrastructure.LSidecarStore.LSidecarEditRead;
        Cadroue.Application.LLibrarian.LLibrarianAudioReader = Cadroue.Infrastructure.LSidecarStore.LSidecarAudioRead;
        Cadroue.Application.LLibrarian.LLibrarianSplitReader = Cadroue.Infrastructure.LSidecarStore.LSidecarSplitRead;
        Cadroue.Application.LLibrarian.LLibrarianFixReader = Cadroue.Infrastructure.LSidecarStore.LSidecarFixRead;
        Cadroue.Application.LLibrarian.LLibrarianDiagnosisReader =
            Cadroue.Infrastructure.LSidecarStore.LSidecarDiagnosisRead;
        Cadroue.Application.LLibrarian.LLibrarianLoudnessReader =
            Cadroue.Infrastructure.LSidecarStore.LSidecarLoudnessRead;
        Cadroue.Application.LLibrarian.LLibrarianDurationReader =
            Cadroue.Infrastructure.LSidecarStore.LSidecarDurationRead;
        Cadroue.Application.LLibrarian.LLibrarianDurationResolver =
            Cadroue.Infrastructure.LSidecarStore.LSidecarDurationResolve;
        Cadroue.Application.LLibrarian.LLibrarianEditWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarEditSave;
        Cadroue.Application.LLibrarian.LLibrarianAudioWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarAudioSave;
        Cadroue.Application.LLibrarian.LLibrarianSplitWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarSplitSave;
        Cadroue.Application.LLibrarian.LLibrarianFixWriter = Cadroue.Infrastructure.LSidecarStore.LSidecarFixSave;
        Cadroue.Application.LLibrarian.LLibrarianDiagnosisWriter =
            Cadroue.Infrastructure.LSidecarStore.LSidecarDiagnosisSave;
        Cadroue.Application.LLibrarian.LLibrarianLoudnessWriter =
            Cadroue.Infrastructure.LSidecarStore.LSidecarLoudnessSave;
        Cadroue.Application.LLibrarian.LLibrarianWaveformWriter =
            Cadroue.Infrastructure.LSidecarStore.LSidecarWaveformSave;
        Cadroue.Application.LLibrarian.LLibrarianFileChecker = Cadroue.Infrastructure.LSidecarStore.LSidecarFileCheck;
        Cadroue.Application.LLibrarian.LLibrarianSourceResolver = LSidecarSourceResolve;
        Cadroue.Application.LLibrarian.LLibrarianSourceMatcher = LSidecarStoreMatch;
        Cadroue.Infrastructure.LCheckup.LCheckupScannerSeam = Cadroue.ShellEngine.LFlawScan.LFlawScanRun;
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
                LTraceLog.LTraceInfoRecord(
                    $"Worklist recovery: {lScheduleRecovered} interrupted job(s) resolved at startup");
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
