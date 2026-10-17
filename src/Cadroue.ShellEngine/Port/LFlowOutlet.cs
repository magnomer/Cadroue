using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The flow gate's reads and range edits: the preferences it starts and turns from, a media record, a section's names
// and the height the panel was left at as plain values, and the zoom and step over its range. The rig hands in how one
// flow's keyframe and waveform scans are made, and the host how the height is read, since the scanners and the frame
// store live below the engine.
public sealed class LFlowOutlet : LFlowPort
{
    private readonly Func<LRigKeyframe> _lFlowOutletKeyframe;
    private readonly Func<LRigWaveform> _lFlowOutletWaveform;
    private readonly Func<double> _lFlowOutletHeight;

    public LFlowOutlet(Func<LRigKeyframe> lKeyframe, Func<LRigWaveform> lWaveform, Func<double> lHeight)
    {
        ArgumentNullException.ThrowIfNull(lKeyframe);
        ArgumentNullException.ThrowIfNull(lWaveform);
        ArgumentNullException.ThrowIfNull(lHeight);
        _lFlowOutletKeyframe = lKeyframe;
        _lFlowOutletWaveform = lWaveform;
        _lFlowOutletHeight = lHeight;
    }

    public bool LFlowWaveformRead() => LPreference.LPreferenceStateCurrent.LPreferenceWaveform;

    // The map row leads unless the preference puts the viewfinder first.
    public bool LFlowOrderRead() => LPreference.LPreferenceStateCurrent.LPreferenceTimelineOrder != "ViewfinderFirst";

    public string LFlowWheelRead() => LPreference.LPreferenceStateCurrent.LPreferenceWheelAction;

    public bool LFlowDragRead() => LPreference.LPreferenceStateCurrent.LPreferenceDragPaused;

    public void LFlowRangeZoom(LSpool lRange, TimeSpan lCursor, int lSteps) => lRange.LSpoolZoom(lCursor, lSteps);

    public TimeSpan LFlowStepResolve(LSpool lRange, int lSteps) => lRange.LSpoolStepResolve(lSteps);

    public (string LFlowName, string LFlowPrefix, string LFlowSuffix)? LFlowNameRead(LSegment lSegment, int lIndex)
    {
        IReadOnlyList<LPiece> lSections = lSegment.LSegmentListRead();
        if (lIndex >= lSections.Count)
        {
            return null;
        }

        LPiece lPiece = lSections[lIndex];
        return (lPiece.LPieceName, lPiece.LPiecePrefix, lPiece.LPieceSuffix);
    }

    public (TimeSpan LFlowDuration, bool LFlowAudio, LSpool LFlowRange) LFlowMediaRead(LMediaInfo lMedia) =>
        (lMedia.LMediaInfoDuration, lMedia.LMediaAudioPresent, new LSpool(lMedia.LMediaInfoDuration));

    public LSectionPort LFlowSectionCreate(Func<string> lSource) => new LSectionOutlet(lSource);

    public LKeyframePort LFlowKeyframeCreate(Func<string> lSource) =>
        new LKeyframeOutlet(lSource, _lFlowOutletKeyframe());

    public LWaveformPort LFlowWaveformCreate() => new LWaveformOutlet(_lFlowOutletWaveform());

    public double LFlowHeightRead() => _lFlowOutletHeight();
}
