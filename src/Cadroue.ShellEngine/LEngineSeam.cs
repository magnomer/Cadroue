using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LEngineSeam
{
    internal static void LEngineSeamAttach(LRig lRig)
    {
        LScheduleContract lSchedule = lRig.LRigSchedule;
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
        LInventoryEngine.LInventoryLayoutSeam = lRig.LRigInventory.LRigInventoryLayout;
        LInventoryEngine.LInventorySampleSeam = lRig.LRigInventory.LRigInventorySample;
        LInventoryEngine.LInventoryEnrollmentSeam = lRig.LRigInventory.LRigInventoryEnrollment;
        LInventoryEngine.LInventoryWithdrawalSeam = lRig.LRigInventory.LRigInventoryWithdrawal;
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
        LStationEngine.LStationSchedule = lSchedule;
        LStationEngine.LStationIntake = lRig.LRigScheduleIntake;
        LStationEngine.LStationLease = lRig.LRigScheduleLease;
        LStationEngine.LStationTransition = lRig.LRigScheduleTransition;
        LStationEngine.LStationMeasurement = lRig.LRigScheduleMeasurement;
        LStationEngine.LStationRemover = lRig.LRigScheduleRemover;
        LStationEngine.LStationProgramSource = lRig.LRigTool.LRigToolProgram;
        LStationEngine.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        LMessenger.LMessengerScheduleSource = () => lSchedule;
        LCartographerSchedule.LCartographerScheduleContract = lSchedule;
        LCartographerAdmission.LCartographerScheduleIntake = lRig.LRigScheduleIntake;
        LCartographerAdmission.LCartographerScheduleRemover = lRig.LRigScheduleRemover;
        LMessenger.LMessengerRouteSource =
            (lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan) =>
                LCartographerAdmission.LCartographerAccept(
                    lMessengerItems, lMessengerTarget, lMessengerSource, lMessengerPlan);
    }

    public static void LEngineStationAttach(Action<Action> lDispatch)
    {
        LStationEngine.LStationPost = lDispatch;
    }
}
