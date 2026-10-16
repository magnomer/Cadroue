using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed class LEngine
{
    public LScheduleContract LEngineSchedule { get; }

    public LEngine(LRig lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);

        LEngineSchedule = lRig.LRigSchedule;
        LTraceEngine.LTraceInfoSeam = lRig.LRigTrace.LRigTraceInfo;
        LTraceEngine.LTraceWarningSeam = lRig.LRigTrace.LRigTraceWarning;
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
        LStation.LStationSchedule = LEngineSchedule;
        LStation.LStationProgramSource = lRig.LRigTool.LRigToolProgram;
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        LMessenger.LMessengerScheduleSource = () => LEngineSchedule;
        LCartographer.LCartographerScheduleContract = LEngineSchedule;
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
