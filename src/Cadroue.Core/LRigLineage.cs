namespace Cadroue.Core;

public sealed record LRigLineage(
    Func<string, Guid> LRigLineageFile,
    Func<LWorkItem, Guid> LRigLineageWork);
