namespace Cadroue.ShellEngine;

internal readonly record struct LAutopsyResult(
    int LAutopsyResultCode,
    string LAutopsyResultSimple,
    string LAutopsyResultTechnical,
    string? LAutopsyResultAction,
    string LAutopsyResultCategory,
    string LAutopsyResultSeverity,
    bool LAutopsyResultUserVisible,
    string LAutopsyResultRetryable,
    string? LAutopsyResultSymbol,
    bool LAutopsyResultMatched);
