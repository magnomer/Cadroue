namespace Cadroue.Core;

public sealed record LRig(
    LRigTrace LRigTrace,
    LRigUsher LRigUsher,
    LRigDepot LRigDepot,
    LRigTool LRigTool,
    LRigEmployer LRigEmployer,
    LRigMedia LRigMedia,
    LRigInventory LRigInventory,
    LRigSentinel LRigSentinel,
    LRigCheckup LRigCheckup,
    LRigLineage LRigLineage,
    LRigPreview LRigPreview,
    LRigFlow LRigFlow,
    LScheduleContract LRigSchedule,
    LScheduleIntakeContract LRigScheduleIntake,
    LScheduleLeaseContract LRigScheduleLease,
    LScheduleTransitionContract LRigScheduleTransition,
    LScheduleMeasurementContract LRigScheduleMeasurement,
    LScheduleRemoverContract LRigScheduleRemover);
