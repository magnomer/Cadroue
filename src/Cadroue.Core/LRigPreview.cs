namespace Cadroue.Core;

public sealed record LRigPreview(
    Func<bool> LRigPreviewFlyleaf,
    Func<LPreviewEngine> LRigPreviewEngine);
