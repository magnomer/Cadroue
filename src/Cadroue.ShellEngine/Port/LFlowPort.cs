using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public interface LFlowPort
{
    bool LFlowWaveformRead();

    bool LFlowOrderRead();

    // What a wheel turn does over the flow, as the preference names it: "Zoom", "Volume" or a seek otherwise.
    string LFlowWheelRead();

    // Whether a drag over the flow pauses the playing media.
    bool LFlowDragRead();

    void LFlowRangeZoom(LSpool lRange, TimeSpan lCursor, int lSteps);

    // How far a seek of this many wheel steps moves over the range.
    TimeSpan LFlowStepResolve(LSpool lRange, int lSteps);

    // A section's names in the store, read through the store unread; null past the last section.
    (string LFlowName, string LFlowPrefix, string LFlowSuffix)? LFlowNameRead(LSegment lSegment, int lIndex);

    // A media record as plain values, with the range a flow keeps over it.
    (TimeSpan LFlowDuration, bool LFlowAudio, LSpool LFlowRange) LFlowMediaRead(LMediaInfo lMedia);

    // One flow's own section store; its traces name the source the callable answers.
    LSectionPort LFlowSectionCreate(Func<string> lSource);

    // One flow's own keyframe scan; its traces name the source the callable answers.
    LKeyframePort LFlowKeyframeCreate(Func<string> lSource);

    // One flow's own waveform scan.
    LWaveformPort LFlowWaveformCreate();

    // The height the flow panel was last left at.
    double LFlowHeightRead();
}
