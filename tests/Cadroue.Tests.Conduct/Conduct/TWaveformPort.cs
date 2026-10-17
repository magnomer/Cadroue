using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TWaveformPort : LWaveformPort
{
    public event Action? LWaveformArrived;

    // The levels the range read answers, whatever the range; empty means no peaks are held, read as null.
    public double[] TWaveformPeaks { get; set; } = [];

    public List<string> TWaveformCalls { get; } = [];

    public void TWaveformRaise() => LWaveformArrived?.Invoke();

    public bool LWaveformPeaksCheck()
    {
        TWaveformCalls.Add("check");
        return TWaveformPeaks.Length > 0;
    }

    public void LWaveformStart(string? lSource, TimeSpan lDuration, bool lAudio) =>
        TWaveformCalls.Add($"start:{lSource}:{lDuration.TotalSeconds}:{lAudio}");

    public void LWaveformClear() => TWaveformCalls.Add("clear");

    public double[]? LWaveformRangeRead(TimeSpan lStart, TimeSpan lEnd, int lColumns)
    {
        TWaveformCalls.Add($"range:{lStart.TotalSeconds}:{lEnd.TotalSeconds}:{lColumns}");
        return TWaveformPeaks.Length == 0 ? null : TWaveformPeaks;
    }

    public void LWaveformClose() => TWaveformCalls.Add("close");
}
