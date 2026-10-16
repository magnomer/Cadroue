namespace Cadroue.Core;

public sealed record LRigTrace(
    Action<string> LRigTraceInfo,
    Action<string> LRigTraceWarning,
    Action<string, Exception?> LRigTraceError,
    Action<string, string?> LRigTraceFfmpeg,
    Func<bool> LRigTraceVerbose,
    Action<bool, string, string?> LRigTraceDetail);
