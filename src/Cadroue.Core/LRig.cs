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
    Func<LRigWaveform> LRigWaveform,
    Func<LRigKeyframe> LRigKeyframe,
    LScheduleContract LRigSchedule);
