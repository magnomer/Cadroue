using System;
using System.Collections.Generic;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TKeyframePort : LKeyframePort
{
    public event Action<LKeyframeNotice>? LKeyframeArrived;

    public IReadOnlyList<LKeyframeEntry> LKeyframeEntries { get; private set; } = [];

    public IReadOnlyList<LKeyframeScanRange> LKeyframeRanges { get; private set; } = [];

    public Func<string>? TKeyframeSource { get; set; }

    public int TKeyframeSerial { get; set; }

    public (bool, bool, TimeSpan?) TKeyframeAnswer { get; set; } = (false, true, null);

    public List<string> TKeyframeCalls { get; } = [];

    public void TKeyframeRaise(LKeyframeNotice notice) => LKeyframeArrived?.Invoke(notice);

    public TimeSpan LKeyframeDelayRead()
    {
        TKeyframeCalls.Add("delay");
        return TimeSpan.FromSeconds(2);
    }

    public void LKeyframeStart(string lSource, LMediaInfo lMedia, TimeSpan lCursor, TimeSpan lDuration) =>
        TKeyframeCalls.Add($"start:{lSource}:{lCursor.TotalSeconds}:{lDuration.TotalSeconds}");

    public void LKeyframeSuspend() => TKeyframeCalls.Add("suspend");

    public void LKeyframeSync(TimeSpan lCursor) => TKeyframeCalls.Add($"sync:{lCursor.TotalSeconds}");

    public bool LKeyframeApply(LKeyframeNotice lNotice)
    {
        TKeyframeCalls.Add($"apply:{lNotice.LKeyframeSerial}");
        if (lNotice.LKeyframeSerial != TKeyframeSerial)
        {
            return false;
        }

        LKeyframeEntries = lNotice.LKeyframeList;
        LKeyframeRanges = lNotice.LKeyframeRanges;
        return true;
    }

    public void LKeyframeClear()
    {
        TKeyframeCalls.Add("clear");
        LKeyframeEntries = [];
        LKeyframeRanges = [];
    }

    public (bool LKeyframeFailed, bool LKeyframeReady, TimeSpan? LKeyframeTarget) LKeyframeMove(
        int lDirection, TimeSpan lCursor)
    {
        TKeyframeCalls.Add($"move:{lDirection}:{lCursor.TotalSeconds}");
        return TKeyframeAnswer;
    }

    public void LKeyframeClose() => TKeyframeCalls.Add("close");
}
