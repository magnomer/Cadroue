using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed class LEngine
{
    public LScheduleContract LEngineSchedule { get; }

    public LVideoFacade LEngineVideo { get; }

    public LAudioFacade LEngineAudio { get; }

    public LFilterFacade LEngineFilter { get; }

    public LWhitebalanceFacade LEngineWhitebalance { get; }

    public LLoudnessFacade LEngineLoudness { get; }

    public LEqualizerFacade LEngineEqualizer { get; }

    public LNoiseFacade LEngineNoise { get; }

    public LConvertFacade LEngineConvert { get; }

    public LRosterFacade LEngineRoster { get; }

    public LRosterCardFacade LEngineRosterCard { get; }

    public LWorkspaceFacade LEngineWorkspace { get; }

    public LUsherFacade LEngineUsher { get; }

    public LEditFacade LEngineEdit { get; }

    public LEditAudioFacade LEngineEditAudio { get; }

    public LToneFacade LEngineTone { get; }

    public LViewerFacade LEngineViewer { get; }

    public LMergeFacade LEngineMerge { get; }

    public LConsoleFacade LEngineConsole { get; }

    public LMapFacade LEngineMap { get; }

    public LRelayFacade LEngineRelay { get; }

    public LLineageFacade LEngineLineage { get; }

    public LRosterDetailFacade LEngineRosterDetail { get; }

    public LFlowFacade LEngineFlow { get; }

    public LEngine(LRig lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);

        LEngineSchedule = lRig.LRigSchedule;
        LEngineVideo = new LVideoFacade();
        LEngineAudio = new LAudioFacade();
        LEngineFilter = new LFilterFacade();
        LEngineWhitebalance = new LWhitebalanceFacade();
        LEngineLoudness = new LLoudnessFacade();
        LEngineEqualizer = new LEqualizerFacade();
        LEngineNoise = new LNoiseFacade();
        LEngineConvert = new LConvertFacade();
        LEngineRoster = new LRosterFacade();
        LEngineRosterCard = new LRosterCardFacade();
        LEngineWorkspace = new LWorkspaceFacade();
        LEngineUsher = new LUsherFacade();
        LEngineEdit = new LEditFacade();
        LEngineEditAudio = new LEditAudioFacade(lRig.LRigTrace);
        LEngineTone = new LToneFacade(lRig.LRigPreview);
        LEngineViewer = new LViewerFacade(lRig.LRigTrace);
        LEngineMerge = new LMergeFacade(lRig.LRigTrace);
        LEngineConsole = new LConsoleFacade(lRig.LRigTool);
        LEngineMap = new LMapFacade(lRig.LRigTrace);
        LEngineRelay = new LRelayFacade(lRig.LRigMedia, lRig.LRigTrace);
        LEngineLineage = new LLineageFacade();
        LEngineRosterDetail = new LRosterDetailFacade();
        LEngineFlow = new LFlowFacade(lRig.LRigFlow);
        LTraceEngine.LTraceInfoSeam = lRig.LRigTrace.LRigTraceInfo;
        LTraceEngine.LTraceWarningSeam = lMessage => lRig.LRigTrace.LRigTraceWarning(lMessage, null);
        LTraceEngine.LTraceErrorSeam = lRig.LRigTrace.LRigTraceError;
        LTraceEngine.LTraceFfmpegSeam = lRig.LRigTrace.LRigTraceFfmpeg;
        LTraceEngine.LTraceVerboseSeam = lRig.LRigTrace.LRigTraceVerbose;
        LTraceEngine.LTraceDetailSeam = lRig.LRigTrace.LRigTraceDetail;
        LDepotEngine.LDepotAudioSeam = lRig.LRigDepot.LRigDepotAudio;
        LDepotEngine.LDepotMergeSeam = lRig.LRigDepot.LRigDepotMerge;
        LDepotEngine.LDepotBridgeSeam = lRig.LRigDepot.LRigDepotBridge;
        LDepotEngine.LDepotPassSeam = lRig.LRigDepot.LRigDepotPass;
        LDepotEngine.LDepotRootSeam = lRig.LRigDepot.LRigDepotRoot;
        LInventoryEngine.LInventoryFilterSeam = lRig.LRigInventory.LRigInventoryFilter;
        LCartographer.LCartographerOwnerSeam = lRig.LRigSentinel.LRigSentinelOwner;
        LRunner.LRunnerAddSeam = lRig.LRigSentinel.LRigSentinelEnrollment;
        LRunner.LRunnerRemoveSeam = lRig.LRigSentinel.LRigSentinelWithdrawal;
        LRunnerLease.LRunnerLeaseSeam = lRig.LRigSentinel.LRigSentinelLease;
        LCheckupEngine.LCheckupReadSeam = lRig.LRigCheckup.LRigCheckupReader;
        LCheckupEngine.LCheckupSaveSeam = lRig.LRigCheckup.LRigCheckupWriter;
        LEmployerEngine.LEmployerRunSeam = lRig.LRigEmployer.LRigEmployerCommand;
        LEmployerEngine.LEmployerListSeam = lRig.LRigEmployer.LRigEmployerList;
        LEmployerEngine.LEmployerSuspendSeam = lRig.LRigEmployer.LRigEmployerSuspension;
        LEmployerEngine.LEmployerResumeSeam = lRig.LRigEmployer.LRigEmployerResumption;
        LEmployerEngine.LEmployerLiveSeam = lRig.LRigEmployer.LRigEmployerLive;
        LToolEngine.LToolFfmpegSeam = lRig.LRigTool.LRigToolFfmpeg;
        LToolEngine.LToolFfprobeSeam = lRig.LRigTool.LRigToolFfprobe;
        LMediaEngine.LMediaProbeSeam = lRig.LRigMedia.LRigMediaProbe;
        LMediaEngine.LMediaLoudnessSeam = lRig.LRigMedia.LRigMediaLoudness;
        LKeyframeEngine.LKeyframeScanSeam = lRig.LRigMedia.LRigMediaKeyframe;
        LSweepEngine.LSweepClaimSeam = lRig.LRigMedia.LRigMediaAdmission;
        LSweepEngine.LSweepReleaseSeam = lRig.LRigMedia.LRigMediaDischarge;
        LUsherDiskEngine.LUsherFileSeam = lRig.LRigUsher.LRigUsherFile;
        LUsherDiskEngine.LUsherFolderSeam = lRig.LRigUsher.LRigUsherFolder;
        LUsherEnvironmentEngine.LUsherProgramSeam = lRig.LRigUsher.LRigUsherProgram;
        LUsherEnvironmentEngine.LUsherPathSeam = lRig.LRigUsher.LRigUsherPath;
        LUsherTextEngine.LUsherNameSeam = lRig.LRigUsher.LRigUsherName;
        LUsherTextEngine.LUsherStemSeam = lRig.LRigUsher.LRigUsherStem;
        LUsherTextEngine.LUsherExtensionSeam = lRig.LRigUsher.LRigUsherExtension;
        LUsherEngine.LUsherRevealSeam = lRig.LRigUsher.LRigUsherReveal;
        LUsherEngine.LUsherExplorerSeam = lRig.LRigUsher.LRigUsherExplorer;
        LUsherEngine.LUsherLinkSeam = lRig.LRigUsher.LRigUsherLink;
        LLineageEngine.LLineageFileSeam = lRig.LRigLineage.LRigLineageFile;
        LLineageEngine.LLineageWorkSeam = lRig.LRigLineage.LRigLineageWork;
        LStation.LStationSchedule = LEngineSchedule;
        LStation.LStationIntake = lRig.LRigScheduleIntake;
        LStation.LStationLease = lRig.LRigScheduleLease;
        LStation.LStationTransition = lRig.LRigScheduleTransition;
        LStation.LStationMeasurement = lRig.LRigScheduleMeasurement;
        LStation.LStationRemover = lRig.LRigScheduleRemover;
        LStation.LStationProgramSource = lRig.LRigTool.LRigToolProgram;
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        LMessenger.LMessengerScheduleSource = () => LEngineSchedule;
        LCartographer.LCartographerScheduleContract = LEngineSchedule;
        LCartographer.LCartographerScheduleIntake = lRig.LRigScheduleIntake;
        LCartographer.LCartographerScheduleRemover = lRig.LRigScheduleRemover;
        LMessenger.LMessengerRouteSource =
            (lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan) =>
                LCartographer.LCartographerAccept(
                    lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan);
    }

    public void LEngineStationAttach(Action<Action> lDispatch)
    {
        LStation.LStationPost = lDispatch;
    }
}
