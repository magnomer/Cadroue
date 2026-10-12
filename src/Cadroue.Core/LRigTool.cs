namespace Cadroue.Core;

public sealed record LRigTool(
    Func<string> LRigToolFfmpeg,
    Func<string> LRigToolFfprobe,
    Func<string> LRigToolProgram);
