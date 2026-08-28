namespace Cadroue.Core;

public enum LWorkPhase
{
    LWorkPhaseNone,
    LWorkPhaseStarted,
    LWorkPhaseEncoding
}

public enum LWorkStage
{
    LWorkStageNone,
    LWorkStageEncode,
    LWorkStageExtract,
    LWorkStageAnalyze,
    LWorkStageProcess,
    LWorkStageSplice,
    LWorkStageMux,
    LWorkStageDuplicate,
    LWorkStageVerify
}
