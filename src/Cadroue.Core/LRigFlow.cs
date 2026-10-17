namespace Cadroue.Core;

public sealed record LRigFlow(
    Func<LRigKeyframe> LRigFlowKeyframe,
    Func<LRigWaveform> LRigFlowWaveform,
    Func<double> LRigFlowHeight);
