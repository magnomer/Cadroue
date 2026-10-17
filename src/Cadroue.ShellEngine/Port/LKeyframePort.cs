using Cadroue.Core;

namespace Cadroue.ShellEngine;

public interface LKeyframePort
{
    // Told on the scanner's thread when a scan answers.
    event Action<LKeyframeNotice>? LKeyframeArrived;

    IReadOnlyList<LKeyframeEntry> LKeyframeEntries { get; }

    IReadOnlyList<LKeyframeScanRange> LKeyframeRanges { get; }

    TimeSpan LKeyframeDelayRead();

    void LKeyframeStart(string lSource, LMediaInfo lMedia, TimeSpan lCursor, TimeSpan lDuration);

    void LKeyframeSuspend();

    void LKeyframeSync(TimeSpan lCursor);

    // A notice of the current scan is traced and kept, and answers true; a stale one is dropped.
    bool LKeyframeApply(LKeyframeNotice lNotice);

    void LKeyframeClear();

    // The move toward a direction: below zero the previous keyframe, above zero the next, zero the nearest.
    (bool LKeyframeFailed, bool LKeyframeReady, TimeSpan? LKeyframeTarget) LKeyframeMove(
        int lDirection, TimeSpan lCursor);

    void LKeyframeClose();
}
