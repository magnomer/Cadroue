using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The flow gate's engine side: the preferences it starts and turns from, a media record, a section's names and the
// height the panel was left at as plain values, and the zoom and step over its range. Each flow's own section store,
// keyframe scan and waveform scan are made here; the rig's flow record hands in how one flow's scanners are made and
// how the height is read, since the scanners and the frame store live below the engine.
public sealed class LFlowFacade
{
    private readonly LRigFlow _lFlowFacadeRig;

    internal LFlowFacade(LRigFlow lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);
        ArgumentNullException.ThrowIfNull(lRig.LRigFlowKeyframe);
        ArgumentNullException.ThrowIfNull(lRig.LRigFlowWaveform);
        ArgumentNullException.ThrowIfNull(lRig.LRigFlowHeight);
        _lFlowFacadeRig = lRig;
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

    public LSectionFacade LFlowSectionCreate(Func<string> lSource) => new(lSource);

    public LKeyframeFacade LFlowKeyframeCreate(Func<string> lSource) =>
        new(lSource, _lFlowFacadeRig.LRigFlowKeyframe());

    public LWaveformFacade LFlowWaveformCreate() => new(_lFlowFacadeRig.LRigFlowWaveform());

    public double LFlowHeightRead() => _lFlowFacadeRig.LRigFlowHeight();
}
