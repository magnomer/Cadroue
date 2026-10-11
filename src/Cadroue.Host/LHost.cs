using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using FlyleafLib;

int code = 1;
Thread thread = new(() =>
{
    PProgram application = new();
    LProgram program = new(new CAtelier(new LUsherOutlet()));
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

    LLocalization.LLocalizationTraceSeam = LTraceLog.LTraceErrorRecord;
    LMediaTrace.LMediaErrorSeam = LTraceLog.LTraceErrorRecord;
    LMediaTrace.LMediaOpenSeam = LTraceLog.LTraceInfoRecord;
    LClassifier.LClassifierFaultSource = lClassifierFault => LTraceLog.LTraceWarningRecord(lClassifierFault);
    LPreset.LPresetTraceSeam = lPresetMessage => LTraceLog.LTraceWarningRecord(lPresetMessage);
    LPreference.LPreferenceLanguageSeam = LLocalization.LLocalizationLanguageNormalize;
    LPreference.LPreferenceLoadSeam = LPreferenceStateStore.LPreferenceStateLoad;
    LPreference.LPreferenceSaveSeam = LPreferenceStateStore.LPreferenceStateSave;
    LPreference.LPreferenceTraceSeam = LTraceLog.LTraceInfoRecord;
    LLibrarian.LLibrarianCoreReader = LSidecarStore.LSidecarCoreRead;
    LLibrarian.LLibrarianKeyframesSeam = LSidecarStore.LSidecarKeyframesRead;
    LLibrarian.LLibrarianWaveformReader = LSidecarStore.LSidecarWaveformRead;
    LLibrarian.LLibrarianEditReader = LSidecarStore.LSidecarEditRead;
    LLibrarian.LLibrarianAudioReader = LSidecarStore.LSidecarAudioRead;
    LLibrarian.LLibrarianSplitReader = LSidecarStore.LSidecarSplitRead;
    LLibrarian.LLibrarianFixReader = LSidecarStore.LSidecarFixRead;
    LLibrarian.LLibrarianDiagnosisReader = LSidecarStore.LSidecarDiagnosisRead;
    LLibrarian.LLibrarianLoudnessReader = LSidecarStore.LSidecarLoudnessRead;
    LLibrarian.LLibrarianDurationReader = LSidecarStore.LSidecarDurationRead;
    LLibrarian.LLibrarianDurationResolver = LSidecarStore.LSidecarDurationResolve;
    LLibrarian.LLibrarianEditWriter = LSidecarStore.LSidecarEditSave;
    LLibrarian.LLibrarianAudioWriter = LSidecarStore.LSidecarAudioSave;
    LLibrarian.LLibrarianSplitWriter = LSidecarStore.LSidecarSplitSave;
    LLibrarian.LLibrarianFixWriter = LSidecarStore.LSidecarFixSave;
    LLibrarian.LLibrarianDiagnosisWriter = LSidecarStore.LSidecarDiagnosisSave;
    LLibrarian.LLibrarianLoudnessWriter = LSidecarStore.LSidecarLoudnessSave;
    LLibrarian.LLibrarianWaveformWriter = LSidecarStore.LSidecarWaveformSave;
    LLibrarian.LLibrarianFileChecker = LSidecarStore.LSidecarFileCheck;
    LLibrarian.LLibrarianSourceResolver = LSidecarStore.LSidecarSourceResolve;
    LLibrarian.LLibrarianSourceMatcher = LSidecarStore.LSidecarSourceMatch;
    LSegment.LSegmentLoadSeam = LSidecarStore.LSidecarSectionsRead;
    LSegment.LSegmentSaveSeam = LSidecarStore.LSidecarSectionsSave;
    LPreset.LPresetNativeSeam = LPresetStore.LPresetNativeLoad;
    LPreset.LPresetLoadSeam = LPresetStore.LPresetLoad;
    LPreset.LPresetSaveSeam = LPresetStore.LPresetSave;
    LPresetSelection.LPresetLoadSeam = LPreset.LPresetSelectionRead;
    LPresetSelection.LPresetSaveSeam = LPreset.LPresetSelectionSave;
    LPresetSelection.LPresetRenameSeam = LPreset.LPresetSelectionSet;
    LPresetSelection.LPresetOutputSeam = LPreset.LPresetSelectionResolve;

    LTraceEngine.LTraceInfoSeam = LTraceLog.LTraceInfoRecord;
    LTraceEngine.LTraceWarningSeam = message => LTraceLog.LTraceWarningRecord(message);
    LTraceEngine.LTraceErrorSeam = LTraceLog.LTraceErrorRecord;
    LTraceEngine.LTraceFfmpegSeam = LTraceLog.LTraceFfmpegRecord;
    LTraceEngine.LTraceVerboseSeam = () => LTrace.LTraceVerbose;
    LDepotEngine.LDepotAudioSeam = LDepot.LDepotAudioRead;
    LDepotEngine.LDepotMergeSeam = LDepot.LDepotMergeRead;
    LDepotEngine.LDepotBridgeSeam = LDepot.LDepotBridgeRead;
    LDepotEngine.LDepotPassSeam = LDepot.LDepotPassRead;
    LDepotEngine.LDepotRootSeam = LDepot.LDepotRootRead;
    LInventoryEngine.LInventoryFilterSeam = LInventory.LInventoryFilterExist;
    LCartographer.LCartographerOwnerSeam = LSentinel.LSentinelOwnerCheck;
    LRunner.LRunnerAddSeam = LSentinel.LSentinelRunnerAdd;
    LRunner.LRunnerRemoveSeam = LSentinel.LSentinelRunnerRemove;
    LRunner.LRunnerLeaseSeam = LSchedule.LScheduleLeaseSeconds;
    LCheckupEngine.LCheckupReadSeam = LCheckup.LCheckupCachedRead;
    LCheckupEngine.LCheckupSaveSeam = LCheckup.LCheckupCachedSave;
    LCheckup.LCheckupScannerSeam = LFlawScanner.LFlawScanRun;
    LEmployerEngine.LEmployerRunSeam =
        (program, prefix, family, level, idle, arguments, token, work, output, error) =>
            new LEmployer(program, prefix)
            {
                LEmployerFamily = family,
                LEmployerLevel = level,
                LEmployerIdleLimit = idle
            }.LEmployerRun(arguments, token, work, output, error);
    LEmployerEngine.LEmployerListSeam =
        (program, family, level, idle, arguments, token, output) =>
            new LEmployer(program)
            {
                LEmployerFamily = family,
                LEmployerLevel = level,
                LEmployerIdleLimit = idle
            }.LEmployerRun(arguments, token, output);
    LEmployerEngine.LEmployerSuspendSeam = LEmployerChild.LEmployerChildSuspend;
    LEmployerEngine.LEmployerResumeSeam = LEmployerChild.LEmployerChildResume;
    LEmployerEngine.LEmployerLiveSeam = LEmployerChild.LEmployerChildCheck;
    LToolEngine.LToolFfmpegSeam = LTool.LToolFfmpegRead;
    LToolEngine.LToolFfprobeSeam = LTool.LToolFfprobeRead;
    LMediaEngine.LMediaProbeSeam = LMedia.LMediaFfprobeRead;
    LMediaEngine.LMediaLoudnessSeam = LMedia.LMediaLoudnessRead;
    LKeyframeEngine.LKeyframeScanSeam = LKeyframeSeeker.LKeyframeRangeScan;
    LSweepEngine.LSweepClaimSeam = LMedia.LMediaScanClaim;
    LSweepEngine.LSweepReleaseSeam = LMedia.LMediaScanRelease;
    LRenderer.LRendererFolderSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;
    LRenderer.LRendererEngineSeam = () => LPreference.LPreferenceStateCurrent.LPreferencePreviewEngine;
    LRenderer.LRendererTextSeam = LLocalization.LLocalizationTextRead;
    LSectionPalette.LSectionPaletteSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceSectionPalette;
    LSchedule.LScheduleSharedSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;
    LSchedule.LScheduleTimeSeam = LGate.LGateTimeRead;
    LRetentionSweep.LRetentionSweptSeam = LRetention.LRetentionSweptCheck;
    LRetentionSweep.LRetentionExpiredSeam = LRetention.LRetentionExpiredCheck;
    LSidecarStore.LSidecarCoreSeam = LLibrarian.LLibrarianLoad;
    LUsherEngine.LUsherFileSeam = LUsher.LUsherFileExist;
    LUsherEngine.LUsherFolderSeam = LUsher.LUsherFolderExist;
    LUsherEngine.LUsherNameSeam = LUsher.LUsherNameRead;
    LUsherEngine.LUsherStemSeam = LUsher.LUsherStemRead;
    LUsherEngine.LUsherExtensionSeam = LUsher.LUsherExtensionRead;
    LUsherEngine.LUsherProgramSeam = LUsher.LUsherProgramResolve;
    LUsherEngine.LUsherPathSeam = LUsher.LUsherPathResolve;
    LUsherEngine.LUsherRevealSeam = LUsher.LUsherPathOpen;
    LUsherEngine.LUsherExplorerSeam = LUsher.LUsherFolderOpen;
    LUsherEngine.LUsherLinkSeam = LUsher.LUsherLinkOpen;

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
        application.MainWindow = new PWindow(new LStrip(program));
        application.MainWindow.Show();
    };
    application.Exit += (_, e) => program.LProgramClose(e.ApplicationExitCode);

    code = application.Run();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return code;
