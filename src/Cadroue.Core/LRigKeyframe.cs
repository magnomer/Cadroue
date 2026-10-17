namespace Cadroue.Core;

// One flow's keyframe scanner as callables, with the trace kinds its scan records. Each move answers
// (ready, target, failed); the disposal detaches the handler it is given, then disposes the scanner.
public sealed record LRigKeyframe(
    Action<string, LMediaInfo, TimeSpan> LRigKeyframeScanner,
    Action LRigKeyframeSuspension,
    Action<TimeSpan> LRigKeyframeCursor,
    Func<int> LRigKeyframeSerial,
    Func<TimeSpan, (bool, TimeSpan?, bool)> LRigKeyframePrevious,
    Func<TimeSpan, (bool, TimeSpan?, bool)> LRigKeyframeNearest,
    Func<TimeSpan, (bool, TimeSpan?, bool)> LRigKeyframeNext,
    Action<Action<LKeyframeNotice>> LRigKeyframeAttachment,
    Action<Action<LKeyframeNotice>> LRigKeyframeDisposal,
    Action<string, string> LRigKeyframeWork,
    Action<string, string> LRigKeyframeWarning);
