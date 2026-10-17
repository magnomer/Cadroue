namespace Cadroue.Core;

// One flow's waveform scanner as callables: the scan over a source, duration and audio presence, the peaks it holds,
// the levels of a range of peaks per column, and its notice. The disposal detaches the notice, then disposes.
public sealed record LRigWaveform(
    Action<string?, TimeSpan, bool> LRigWaveformScanner,
    Action LRigWaveformClearance,
    Func<byte[]> LRigWaveformPeaks,
    Func<byte[], TimeSpan, TimeSpan, int, double[]> LRigWaveformRange,
    Action<Action> LRigWaveformAttachment,
    Action LRigWaveformDisposal);
