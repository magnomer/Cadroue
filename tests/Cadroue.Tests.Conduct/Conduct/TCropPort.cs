using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TCropPort : LCropPort
{
    public event Action? LCropChanged;

    public int TCropRotation { get; set; }

    public bool TCropHorizontal { get; set; }

    public bool TCropVertical { get; set; }

    public (bool, bool, bool, bool, int, int) TCropState { get; set; }

    public (int, int, int, int) TCropEdges { get; set; }

    public (int, int, int, int) TCropNormal { get; set; }

    public (bool, double, double) TCropSource { get; set; } = (true, 1920, 1080);

    public bool TCropRect { get; set; } = true;

    public List<string> TCropCalls { get; } = [];

    public void TCropRaise() => LCropChanged?.Invoke();

    public (int LCropRotation, bool LCropHorizontal, bool LCropVertical) LCropOrientationRead() =>
        (TCropRotation, TCropHorizontal, TCropVertical);

    public (bool LCropActive, bool LCropPersistent, bool LCropFixed, bool LCropLenient, int LCropWidth, int LCropHeight)
        LCropStateRead() => TCropState;

    public (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropEdgesRead() => TCropEdges;

    public (int LCropLeft, int LCropTop, int LCropRight, int LCropBottom) LCropNormalRead() => TCropNormal;

    public (bool LCropPresent, double LCropWidth, double LCropHeight) LCropSourceRead() => TCropSource;

    public bool LCropRectCheck() => TCropRect;

    public (bool LCropTolerated, int LCropExcess, bool LCropWide) TCropNotice { get; set; }

    public (bool LCropTolerated, int LCropExcess, bool LCropWide) LCropNoticeRead(double lWidth, double lHeight)
    {
        TCropCalls.Add($"notice {lWidth}x{lHeight}");
        return TCropNotice;
    }

    public (int LCropWidth, int LCropHeight) LCropRatioNormalize(int lWidth, int lHeight)
    {
        TCropCalls.Add($"normalize {lWidth}x{lHeight}");
        return (lWidth / 10, lHeight / 10);
    }

    public void LCropRotateSet(int lRotation) => TCropCalls.Add($"rotate {lRotation}");

    public void LCropFlipSet(bool lHorizontal, bool lFlipped) =>
        TCropCalls.Add($"flip {(lHorizontal ? "horizontal" : "vertical")} {lFlipped}");

    public void LCropEdgeSet(int lSide, double lEdge, bool lLocked) =>
        TCropCalls.Add($"edge {lSide} {lEdge} {lLocked}");

    public void LCropEdgeReset() => TCropCalls.Add("edge reset");

    public void LCropApplySet(bool lApply, bool lRatioReset) => TCropCalls.Add($"apply {lApply} {lRatioReset}");

    public void LCropWorkApply(LInspectorPort lPlan) => TCropCalls.Add("work");

    public void LCropPersistentSet(bool lPersistent) => TCropCalls.Add($"persistent {lPersistent}");

    public void LCropReset() => TCropCalls.Add("reset");

    public void LCropRatioSet(bool lFixed, bool lLenient, int lWidth, int lHeight, bool lUnlock) =>
        TCropCalls.Add($"ratio {lFixed} {lLenient} {lWidth}:{lHeight} {lUnlock}");

    public void LCropRatioReset() => TCropCalls.Add("ratio reset");

    public void LCropPresetSet(int lWidth, int lHeight) => TCropCalls.Add($"preset {lWidth}:{lHeight}");
}
