namespace Cadroue.ShellEngine;

internal readonly record struct LAutopsySpine(
    string? LAutopsySpineSymbol,
    string LAutopsySpineCategory,
    string LAutopsySpineSeverity,
    bool LAutopsySpineVisible,
    string LAutopsySpineRetryable,
    string LAutopsySpineClass);
