using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TViewerPort : LViewerPort
{
    public List<string> TViewerCalls { get; } = [];

    public bool TViewerVerbose { get; set; }

    public bool LViewerVerboseRead() => TViewerVerbose;

    public void LViewerRefreshRecord(string lReason) => TViewerCalls.Add($"refresh:{lReason}");

    public void LViewerRotatedRecord(string lRotate, bool lHorizontal, bool lVertical, bool lReady) =>
        TViewerCalls.Add($"rotated:{lRotate}|{lHorizontal}|{lVertical}|{lReady}");

    public void LViewerNoticeRecord(Exception lFailure) => TViewerCalls.Add($"notice:{lFailure.Message}");

    public void LViewerHostRecord(int lStamp, string lStage, string lDetail) =>
        TViewerCalls.Add($"host:{lStamp}|{lStage}|{lDetail}");

    public void LViewerStageRecord(string lStage, Exception lFailure) =>
        TViewerCalls.Add($"stage:{lStage}|{lFailure.Message}");

    public void LViewerClosedRecord(string lPath) => TViewerCalls.Add($"closed:{lPath}");

    public void LViewerDetachedRecord(bool lReady) => TViewerCalls.Add($"detached:{lReady}");

    public void LViewerLoadRecord(int lSerial, string lPath, string lEngine) =>
        TViewerCalls.Add($"load:{lSerial}|{lPath}|{lEngine}");

    public void LViewerProbeRecord(string lKind, string lPath, string? lError) =>
        TViewerCalls.Add($"probe:{lKind}|{lPath}|{lError}");

    public void LViewerSupersededRecord(int lSerial, bool lUnloaded) =>
        TViewerCalls.Add($"superseded:{lSerial}|{lUnloaded}");

    public void LViewerDiscardedRecord(int lSerial, int lCurrent, bool lUnloaded) =>
        TViewerCalls.Add($"discarded:{lSerial}|{lCurrent}|{lUnloaded}");

    public void LViewerReusedRecord() => TViewerCalls.Add("reused");

    public void LViewerSwappedRecord(bool lReady) => TViewerCalls.Add($"swapped:{lReady}");

    public void LViewerPreviewRecord(string lRotate, bool lHorizontal, bool lVertical) =>
        TViewerCalls.Add($"preview:{lRotate}|{lHorizontal}|{lVertical}");

    public void LViewerRejectedRecord(TimeSpan lPosition, string lReason) =>
        TViewerCalls.Add($"rejected:{lPosition.TotalSeconds}|{lReason}");

    public void LViewerSeekRecord(TimeSpan lPosition, bool lDirect, bool lRunning) =>
        TViewerCalls.Add($"seek:{lPosition.TotalSeconds}|{lDirect}|{lRunning}");

    public string LViewerSeekFormat(TimeSpan lPosition, bool lDirect, bool lRunning) =>
        $"line:{lPosition.TotalSeconds}|{lDirect}|{lRunning}";

    public void LViewerDragRecord(TimeSpan lFinal, IReadOnlyList<string> lLines) =>
        TViewerCalls.Add($"drag:{lFinal.TotalSeconds}|{string.Join(",", lLines)}");

    public void LViewerHiddenRecord(bool lHostVisible, bool lMpvShown, double lWidth, double lHeight) =>
        TViewerCalls.Add($"hidden:{lHostVisible}|{lMpvShown}|{lWidth}|{lHeight}");

    public void LViewerShownRecord(double lWidth, double lHeight) => TViewerCalls.Add($"shown:{lWidth}|{lHeight}");

    public void LViewerRequestedRecord(string lPath, bool lActive, bool lUnloaded, string lEngine) =>
        TViewerCalls.Add($"requested:{lPath}|{lActive}|{lUnloaded}|{lEngine}");

    public void LViewerRefusedRecord(bool lActive) => TViewerCalls.Add($"refused:{lActive}");

    public void LViewerUnresolvedRecord() => TViewerCalls.Add("unresolved");
}
