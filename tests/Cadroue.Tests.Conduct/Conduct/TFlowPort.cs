using System;
using System.Collections.Generic;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TFlowPort : LFlowPort
{
    public bool TFlowWaveform { get; init; }

    public bool TFlowOrder { get; init; } = true;

    public (TimeSpan, bool) TFlowMedia { get; set; } = (TimeSpan.FromSeconds(60), true);

    public string TFlowWheel { get; set; } = "Seek";

    public bool TFlowDrag { get; set; } = true;

    public (string, string, string)? TFlowName { get; set; }

    public List<string> TFlowCalls { get; } = [];

    public TSectionPort TFlowSection { get; } = new();

    public TKeyframePort TFlowKeyframe { get; } = new();

    public TWaveformPort TFlowWaveformPort { get; } = new();

    public double TFlowHeight { get; init; } = 180;

    public bool LFlowWaveformRead()
    {
        TFlowCalls.Add("waveform");
        return TFlowWaveform;
    }

    public bool LFlowOrderRead()
    {
        TFlowCalls.Add("order");
        return TFlowOrder;
    }

    public string LFlowWheelRead()
    {
        TFlowCalls.Add("wheel");
        return TFlowWheel;
    }

    public bool LFlowDragRead()
    {
        TFlowCalls.Add("drag");
        return TFlowDrag;
    }

    public void LFlowRangeZoom(LSpool lRange, TimeSpan lCursor, int lSteps)
    {
        TFlowCalls.Add($"zoom:{lCursor.TotalSeconds}:{lSteps}");
        Interface.TInterface.TSpoolZoom(lRange, lCursor, lSteps);
    }

    public TimeSpan LFlowStepResolve(LSpool lRange, int lSteps)
    {
        TFlowCalls.Add($"step:{lSteps}");
        return TimeSpan.FromSeconds(lSteps * 2);
    }

    public (string LFlowName, string LFlowPrefix, string LFlowSuffix)? LFlowNameRead(LSegment lSegment, int lIndex)
    {
        TFlowCalls.Add($"name:{lIndex}");
        return TFlowName;
    }

    public (TimeSpan LFlowDuration, bool LFlowAudio, LSpool LFlowRange) LFlowMediaRead(LMediaInfo lMedia)
    {
        TFlowCalls.Add("media");
        return (TFlowMedia.Item1, TFlowMedia.Item2, Interface.TInterface.TSpoolCreate(TFlowMedia.Item1));
    }

    public LSectionPort LFlowSectionCreate(Func<string> lSource)
    {
        TFlowSection.TSectionSource = lSource;
        return TFlowSection;
    }

    public LKeyframePort LFlowKeyframeCreate(Func<string> lSource)
    {
        TFlowKeyframe.TKeyframeSource = lSource;
        return TFlowKeyframe;
    }

    public LWaveformPort LFlowWaveformCreate() => TFlowWaveformPort;

    public double LFlowHeightRead()
    {
        TFlowCalls.Add("height");
        return TFlowHeight;
    }
}
